using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.Sql.Idempotence;

public class IdempotenceService : IIdempotenceService
{
    private readonly string _connectionStringName;
    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IScriptProvider _scriptProvider;

    public IdempotenceService(IOptionsMonitor<ConnectionStringOptions> connectionStringOptions, IOptions<ServiceBusOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IScriptProvider scriptProvider, IDatabaseContextFactory databaseContextFactory)
    {
        Guard.AgainstNull(connectionStringOptions);
        Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
        Guard.AgainstNull(serviceBusConfiguration);

        if (serviceBusConfiguration.Inbox?.WorkQueue == null)
        {
            throw new InvalidOperationException(Resources.NoInboxException);
        }

        _scriptProvider = Guard.AgainstNull(scriptProvider);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);

        _connectionStringName = serviceBusOptions.Value.Idempotence.ConnectionStringName;

        using (new DatabaseContextScope())
        using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        using (var transaction = databaseContext.BeginTransactionAsync().GetAwaiter().GetResult())
        {
            if (databaseContext.GetScalarAsync<int>(new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceServiceExists))).GetAwaiter().GetResult() != 1)
            {
                throw new InvalidOperationException(Resources.IdempotenceDatabaseNotConfigured);
            }

            databaseContext.ExecuteAsync(new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceInitialize))
                    .AddParameter(Columns.InboxWorkQueueUri, serviceBusConfiguration.Inbox.WorkQueue.Uri.ToString()))
                .GetAwaiter().GetResult();

            transaction.CommitTransactionAsync().GetAwaiter().GetResult();
        }
    }

    public async ValueTask<bool> AddDeferredMessageAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
        {
            var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceSendDeferredMessage))
                .AddParameter(Columns.MessageId, Guard.AgainstNull(deferredTransportMessage).MessageId)
                .AddParameter(Columns.MessageIdReceived, Guard.AgainstNull(processingTransportMessage).MessageId)
                .AddParameter(Columns.MessageBody, await Guard.AgainstNull(deferredTransportMessageStream).ToBytesAsync().ConfigureAwait(false));

            await databaseContext.ExecuteAsync(query).ConfigureAwait(false);

            await transaction.CommitTransactionAsync().ConfigureAwait(false);
        }

        return true;
    }

    public async Task DeferredMessageSentAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
        {
            var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceDeferredMessageSent))
                .AddParameter(Columns.MessageId, Guard.AgainstNull(deferredTransportMessage).MessageId);

            await databaseContext.ExecuteAsync(query).ConfigureAwait(false);

            await transaction.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task<IEnumerable<Stream>> GetDeferredMessagesAsync(TransportMessage transportMessage)
    {
        var result = new List<Stream>();

        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
        {
            var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceGetDeferredMessages))
                .AddParameter(Columns.MessageIdReceived, Guard.AgainstNull(transportMessage).MessageId);

            var rows = await databaseContext.GetRowsAsync(query);

            foreach (var row in rows)
            {
                result.Add(new MemoryStream((byte[])row["MessageBody"]));
            }

            await transaction.CommitTransactionAsync().ConfigureAwait(false);
        }

        return result;
    }

    public async Task MessageHandledAsync(TransportMessage transportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
        {
            var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceMessageHandled))
                .AddParameter(Columns.MessageId, Guard.AgainstNull(transportMessage).MessageId);

            await databaseContext.ExecuteAsync(query).ConfigureAwait(false);

            await transaction.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task ProcessingCompletedAsync(TransportMessage transportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
        await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
        {
            var query = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceComplete))
                .AddParameter(Columns.MessageId, Guard.AgainstNull(transportMessage).MessageId);

            await databaseContext.ExecuteAsync(query).ConfigureAwait(false);

            await transaction.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask<ProcessingStatus> ProcessingStatusAsync(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        try
        {
            using (new DatabaseContextScope())
            await using (var databaseContext = _databaseContextFactory.Create(_connectionStringName))
            await using (var transaction = await databaseContext.BeginTransactionAsync().ConfigureAwait(false))
            {
                try
                {
                    var hasCompletedQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceHasCompleted))
                        .AddParameter(Columns.MessageId, transportMessage.MessageId);

                    if (await databaseContext.GetScalarAsync<int>(hasCompletedQuery).ConfigureAwait(false) == 1)
                    {
                        return ProcessingStatus.Ignore;
                    }

                    var isProcessingQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceIsProcessing))
                        .AddParameter(Columns.MessageId, transportMessage.MessageId);

                    if (await databaseContext.GetScalarAsync<int>(isProcessingQuery).ConfigureAwait(false) == 1)
                    {
                        return ProcessingStatus.Ignore;
                    }

                    var processingQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceProcessing))
                        .AddParameter(Columns.MessageId, transportMessage.MessageId)
                        .AddParameter(Columns.InboxWorkQueueUri, transportMessage.RecipientInboxWorkQueueUri)
                        .AddParameter(Columns.AssignedThreadId, Environment.CurrentManagedThreadId);

                    await databaseContext.ExecuteAsync(processingQuery).ConfigureAwait(false);

                    var isMessageHandledQuery = new Query(_scriptProvider.Get(_connectionStringName, Script.IdempotenceIsMessageHandled))
                        .AddParameter(Columns.MessageId, transportMessage.MessageId);

                    var messageHandled = await databaseContext.GetScalarAsync<int>(isMessageHandledQuery) == 1;

                    return messageHandled
                        ? ProcessingStatus.MessageHandled
                        : ProcessingStatus.Assigned;
                }
                finally
                {
                    await transaction.CommitTransactionAsync();
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
                return ProcessingStatus.Ignore;
            }

            throw;
        }
    }
}