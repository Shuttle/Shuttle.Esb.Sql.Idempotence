using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.Sql.Idempotence
{
    public class IdempotenceService : IIdempotenceService
    {
        private readonly IDatabaseContextFactory _databaseContextFactory;

        private readonly IDatabaseGateway _databaseGateway;
        private readonly string _idempotenceConnectionString;
        private readonly IdempotenceOptions _idempotenceOptions;

        private readonly string _idempotenceProviderName;
        private readonly IScriptProvider _scriptProvider;

        public IdempotenceService(
            IOptionsMonitor<ConnectionStringOptions> connectionStringOptions,
            IOptions<IdempotenceOptions> idempotenceOptions,
            IServiceBusConfiguration serviceBusConfiguration,
            IScriptProvider scriptProvider,
            IDatabaseContextFactory databaseContextFactory,
            IDatabaseGateway databaseGateway)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));
            Guard.AgainstNull(idempotenceOptions, nameof(idempotenceOptions));
            Guard.AgainstNull(idempotenceOptions.Value, nameof(idempotenceOptions.Value));
            Guard.AgainstNull(serviceBusConfiguration, nameof(serviceBusConfiguration));
            Guard.AgainstNull(scriptProvider, nameof(scriptProvider));
            Guard.AgainstNull(databaseContextFactory, nameof(databaseContextFactory));
            Guard.AgainstNull(databaseGateway, nameof(databaseGateway));

            if (!serviceBusConfiguration.HasInbox())
            {
                throw new InvalidOperationException(Resources.NoInboxException);
            }

            _scriptProvider = scriptProvider;
            _databaseContextFactory = databaseContextFactory;
            _databaseGateway = databaseGateway;

            _idempotenceOptions = idempotenceOptions.Value;

            var connectionStringName = idempotenceOptions.Value.ConnectionStringName;
            var connectionString = connectionStringOptions.Get(connectionStringName);

            if (connectionString == null)
            {
                throw new InvalidOperationException(string.Format(Core.Data.Resources.ConnectionSettingsMissing,
                    connectionStringName));
            }

            _idempotenceProviderName = connectionString.ProviderName;
            _idempotenceConnectionString = connectionString.ConnectionString;

            using (
                var connection = _databaseContextFactory.Create(_idempotenceProviderName,
                    _idempotenceConnectionString))
            using (var transaction = connection.BeginTransaction())
            {
                if (_databaseGateway.GetScalar<int>(
                        RawQuery.Create(
                            _scriptProvider.Get(
                                Script.IdempotenceServiceExists))) != 1)
                {
                    throw new InvalidOperationException(Resources.IdempotenceDatabaseNotConfigured);
                }

                _databaseGateway.Execute(
                    RawQuery.Create(
                            _scriptProvider.Get(
                                Script.IdempotenceInitialize))
                        .AddParameterValue(Columns.InboxWorkQueueUri,
                            serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString()));

                transaction.CommitTransaction();
            }
        }

        public ProcessingStatus ProcessingStatus(TransportMessage transportMessage)
        {
            try
            {
                using (
                    var connection = _databaseContextFactory.Create(_idempotenceProviderName,
                        _idempotenceConnectionString))
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (_databaseGateway.GetScalar<int>(
                                RawQuery.Create(
                                        _scriptProvider.Get(
                                            Script.IdempotenceHasCompleted))
                                    .AddParameterValue(Columns.MessageId, transportMessage.MessageId)) == 1)
                        {
                            return Esb.ProcessingStatus.Ignore;
                        }

                        if (_databaseGateway.GetScalar<int>(
                                RawQuery.Create(
                                        _scriptProvider.Get(
                                            Script.IdempotenceIsProcessing))
                                    .AddParameterValue(Columns.MessageId, transportMessage.MessageId)) == 1)
                        {
                            return Esb.ProcessingStatus.Ignore;
                        }

                        _databaseGateway.Execute(
                            RawQuery.Create(
                                    _scriptProvider.Get(
                                        Script.IdempotenceProcessing))
                                .AddParameterValue(Columns.MessageId, transportMessage.MessageId)
                                .AddParameterValue(Columns.InboxWorkQueueUri,
                                    transportMessage.RecipientInboxWorkQueueUri)
                                .AddParameterValue(Columns.AssignedThreadId,
                                    Thread.CurrentThread.ManagedThreadId));

                        var messageHandled = _databaseGateway.GetScalar<int>(
                            RawQuery.Create(
                                    _scriptProvider.Get(
                                        Script.IdempotenceIsMessageHandled))
                                .AddParameterValue(Columns.MessageId, transportMessage.MessageId)) == 1;

                        return messageHandled
                            ? Esb.ProcessingStatus.MessageHandled
                            : Esb.ProcessingStatus.Assigned;
                    }
                    finally
                    {
                        transaction.CommitTransaction();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message.ToUpperInvariant();

                if (message.Contains("VIOLATION OF UNIQUE KEY CONSTRAINT") ||
                    message.Contains("CANNOT INSERT DUPLICATE KEY") || message.Contains("IGNORE MESSAGE PROCESSING"))
                {
                    return Esb.ProcessingStatus.Ignore;
                }

                throw;
            }
        }

        public void ProcessingCompleted(TransportMessage transportMessage)
        {
            using (
                var connection = _databaseContextFactory.Create(_idempotenceProviderName,
                    _idempotenceConnectionString))
            using (var transaction = connection.BeginTransaction())
            {
                _databaseGateway.Execute(
                    RawQuery.Create(
                            _scriptProvider.Get(Script.IdempotenceComplete))
                        .AddParameterValue(Columns.MessageId, transportMessage.MessageId));

                transaction.CommitTransaction();
            }
        }

        public IEnumerable<Stream> GetDeferredMessages(TransportMessage transportMessage)
        {
            var result = new List<Stream>();

            using (_databaseContextFactory.Create(_idempotenceProviderName, _idempotenceConnectionString))
            {
                var rows = _databaseGateway.GetRows(
                    RawQuery.Create(_scriptProvider.Get(Script.IdempotenceGetDeferredMessages))
                        .AddParameterValue(Columns.MessageIdReceived, transportMessage.MessageId));

                foreach (var row in rows)
                {
                    result.Add(new MemoryStream((byte[])row["MessageBody"]));
                }
            }

            return result;
        }

        public void DeferredMessageSent(TransportMessage processingTransportMessage,
            TransportMessage deferredTransportMessage)
        {
            using (_databaseContextFactory.Create(_idempotenceProviderName, _idempotenceConnectionString))
            {
                _databaseGateway.Execute(
                    RawQuery.Create(_scriptProvider.Get(Script.IdempotenceDeferredMessageSent))
                        .AddParameterValue(Columns.MessageId, deferredTransportMessage.MessageId));
            }
        }

        public void MessageHandled(TransportMessage transportMessage)
        {
            using (_databaseContextFactory.Create(_idempotenceProviderName, _idempotenceConnectionString))
            {
                _databaseGateway.Execute(
                    RawQuery.Create(_scriptProvider.Get(Script.IdempotenceMessageHandled))
                        .AddParameterValue(Columns.MessageId, transportMessage.MessageId));
            }
        }

        public bool AddDeferredMessage(TransportMessage processingTransportMessage,
            TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream)
        {
            using (_databaseContextFactory.Create(_idempotenceProviderName, _idempotenceConnectionString))
            {
                _databaseGateway.Execute(
                    RawQuery.Create(_scriptProvider.Get(Script.IdempotenceSendDeferredMessage))
                        .AddParameterValue(Columns.MessageId, deferredTransportMessage.MessageId)
                        .AddParameterValue(Columns.MessageIdReceived, processingTransportMessage.MessageId)
                        .AddParameterValue(Columns.MessageBody, deferredTransportMessageStream.ToBytes()));
            }

            return true;
        }
    }
}