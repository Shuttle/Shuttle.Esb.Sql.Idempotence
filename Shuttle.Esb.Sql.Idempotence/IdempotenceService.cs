﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.Sql.Idempotence
{
    public class IdempotenceService : IIdempotenceService
    {
        private readonly string _connectionStringName;
        private readonly IDatabaseContextFactory _databaseContextFactory;

        private readonly IDatabaseGateway _databaseGateway;
        private readonly IScriptProvider _scriptProvider;

        public IdempotenceService(
            IOptionsMonitor<ConnectionStringOptions> connectionStringOptions,
            IOptions<ServiceBusOptions> serviceBusOptions,
            IServiceBusConfiguration serviceBusConfiguration,
            IScriptProvider scriptProvider,
            IDatabaseContextFactory databaseContextFactory,
            IDatabaseGateway databaseGateway)
        {
            Guard.AgainstNull(connectionStringOptions, nameof(connectionStringOptions));
            Guard.AgainstNull(serviceBusOptions, nameof(serviceBusOptions));
            Guard.AgainstNull(serviceBusOptions.Value, nameof(serviceBusOptions.Value));
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

            _connectionStringName = serviceBusOptions.Value.Idempotence.ConnectionStringName;

            using (var connection = _databaseContextFactory.Create(_connectionStringName))
            using (var transaction = connection.BeginTransaction())
            {
                if (_databaseGateway.GetScalar<int>(
                        new Query(
                            _scriptProvider.Get(_connectionStringName, Script.IdempotenceServiceExists))) != 1)
                {
                    throw new InvalidOperationException(Resources.IdempotenceDatabaseNotConfigured);
                }

                _databaseGateway.Execute(
                    new Query(
                            _scriptProvider.Get(_connectionStringName, Script.IdempotenceInitialize))
                        .AddParameter(Columns.InboxWorkQueueUri,
                            serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString()));

                transaction.CommitTransaction();
            }
        }

        public ProcessingStatus ProcessingStatus(TransportMessage transportMessage)
        {
            return ProcessingStatusAsync(transportMessage, true).GetAwaiter().GetResult();
        }

        public async ValueTask<ProcessingStatus> ProcessingStatusAsync(TransportMessage transportMessage)
        {
            return await ProcessingStatusAsync(transportMessage, false).ConfigureAwait(false);
        }

        public void ProcessingCompleted(TransportMessage transportMessage)
        {
            ProcessingCompletedAsync(transportMessage, true).GetAwaiter().GetResult();
        }

        public async Task ProcessingCompletedAsync(TransportMessage transportMessage)
        {
            await ProcessingCompletedAsync(transportMessage, false).ConfigureAwait(false);
        }

        private async Task ProcessingCompletedAsync(TransportMessage transportMessage, bool sync)
        {
            using (var connection = _databaseContextFactory.Create(_connectionStringName))
            using (var transaction = sync ? connection.BeginTransaction() : await connection.BeginTransactionAsync().ConfigureAwait(false))
            {
                var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceComplete))
                    .AddParameter(Columns.MessageId, transportMessage.MessageId);

                if (sync)
                {
                    _databaseGateway.Execute(query);

                    transaction.CommitTransaction();
                }
                else
                {
                    await _databaseGateway.ExecuteAsync(query).ConfigureAwait(false);

                    await transaction.CommitTransactionAsync().ConfigureAwait(false);
                }
            }
        }

        public async ValueTask<bool> AddDeferredMessageAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream)
        {
            return await AddDeferredMessageAsync(processingTransportMessage, deferredTransportMessage, deferredTransportMessageStream, false).ConfigureAwait(false);
        }

        private async ValueTask<bool> AddDeferredMessageAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream, bool sync)
        {
            using (_databaseContextFactory.Create(_connectionStringName))
            {
                var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceSendDeferredMessage))
                    .AddParameter(Columns.MessageId, deferredTransportMessage.MessageId)
                    .AddParameter(Columns.MessageIdReceived, processingTransportMessage.MessageId)
                    .AddParameter(Columns.MessageBody,  sync? deferredTransportMessageStream.ToBytes(): await deferredTransportMessageStream.ToBytesAsync().ConfigureAwait(false));

                if (sync)
                {
                    _databaseGateway.Execute(query);
                }
                else
                {
                    await _databaseGateway.ExecuteAsync(query).ConfigureAwait(false);
                }
            }

            return true;
        }

        public IEnumerable<Stream> GetDeferredMessages(TransportMessage transportMessage)
        {
            return GetDeferredMessagesAsync(transportMessage, true).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<Stream>> GetDeferredMessagesAsync(TransportMessage transportMessage)
        {
            return await GetDeferredMessagesAsync(transportMessage, false).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Stream>> GetDeferredMessagesAsync(TransportMessage transportMessage, bool sync)
        {
            var result = new List<Stream>();

            using (_databaseContextFactory.Create(_connectionStringName))
            {
                var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceGetDeferredMessages))
                    .AddParameter(Columns.MessageIdReceived, transportMessage.MessageId);

                var rows = sync
                ?_databaseGateway.GetRows(query)
                : await _databaseGateway.GetRowsAsync(query);

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
            using (_databaseContextFactory.Create(_connectionStringName))
            {
                _databaseGateway.Execute(
                    new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceDeferredMessageSent))
                        .AddParameter(Columns.MessageId, deferredTransportMessage.MessageId));
            }
        }

        public Task DeferredMessageSentAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage)
        {
            throw new NotImplementedException();
        }

        public void MessageHandled(TransportMessage transportMessage)
        {
            using (_databaseContextFactory.Create(_connectionStringName))
            {
                _databaseGateway.Execute(
                    new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceMessageHandled))
                        .AddParameter(Columns.MessageId, transportMessage.MessageId));
            }
        }

        public Task MessageHandledAsync(TransportMessage transportMessage)
        {
            throw new NotImplementedException();
        }

        public bool AddDeferredMessage(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream)
        {
            return AddDeferredMessageAsync(processingTransportMessage, deferredTransportMessage, deferredTransportMessageStream, true).GetAwaiter().GetResult();
        }

        private async ValueTask<ProcessingStatus> ProcessingStatusAsync(TransportMessage transportMessage, bool sync)
        {
            try
            {
                using (var connection = _databaseContextFactory.Create(_connectionStringName))
                using (var transaction = sync ? connection.BeginTransaction() : await connection.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        var hasCompletedQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceHasCompleted))
                            .AddParameter(Columns.MessageId, transportMessage.MessageId);

                        if ((sync ? _databaseGateway.GetScalar<int>(hasCompletedQuery) : await _databaseGateway.GetScalarAsync<int>(hasCompletedQuery).ConfigureAwait(false)) == 1)
                        {
                            return Esb.ProcessingStatus.Ignore;
                        }

                        var isProcessingQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceIsProcessing))
                            .AddParameter(Columns.MessageId, transportMessage.MessageId);

                        if ((sync ? _databaseGateway.GetScalar<int>(isProcessingQuery) : await _databaseGateway.GetScalarAsync<int>(isProcessingQuery).ConfigureAwait(false)) == 1)
                        {
                            return Esb.ProcessingStatus.Ignore;
                        }

                        var processingQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceProcessing))
                            .AddParameter(Columns.MessageId, transportMessage.MessageId)
                            .AddParameter(Columns.InboxWorkQueueUri, transportMessage.RecipientInboxWorkQueueUri)
                            .AddParameter(Columns.AssignedThreadId, Thread.CurrentThread.ManagedThreadId);

                        if (sync)
                        {
                            _databaseGateway.Execute(processingQuery);
                        }
                        else
                        {
                            await _databaseGateway.ExecuteAsync(processingQuery).ConfigureAwait(false);
                        }

                        var isMessageHandledQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceIsMessageHandled))
                            .AddParameter(Columns.MessageId, transportMessage.MessageId);

                        var messageHandled = (sync ? _databaseGateway.GetScalar<int>(isMessageHandledQuery) : await _databaseGateway.GetScalarAsync<int>(isMessageHandledQuery)) == 1;

                        return messageHandled
                            ? Esb.ProcessingStatus.MessageHandled
                            : Esb.ProcessingStatus.Assigned;
                    }
                    finally
                    {
                        if (sync)
                        {
                            transaction.CommitTransaction();
                        }
                        else
                        {
                            await transaction.CommitTransactionAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message.ToUpperInvariant();

                if (message.Contains("VIOLATION OF UNIQUE KEY CONSTRAINT", StringComparison.InvariantCultureIgnoreCase) ||
                    message.Contains("CANNOT INSERT DUPLICATE KEY", StringComparison.InvariantCultureIgnoreCase) || 
                    message.Contains("IGNORE MESSAGE PROCESSING", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Esb.ProcessingStatus.Ignore;
                }

                throw;
            }
        }
    }
}