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
    private readonly IDatabaseContextFactory _databaseContextFactory;
    private readonly IQueryFactory _queryFactory;
    private readonly SqlIdempotenceOptions _sqlIdempotenceOptions;

    public IdempotenceService(IOptions<SqlIdempotenceOptions> sqlIdempotenceOptions, IServiceBusConfiguration serviceBusConfiguration, IQueryFactory queryFactory, IDatabaseContextFactory databaseContextFactory)
    {
        _sqlIdempotenceOptions = Guard.AgainstNull(Guard.AgainstNull(sqlIdempotenceOptions).Value);
        _queryFactory = Guard.AgainstNull(queryFactory);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);
    }

    public async ValueTask<bool> AddDeferredMessageAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage, Stream deferredTransportMessageStream)
    {
        Guard.AgainstNull(processingTransportMessage);
        Guard.AgainstNull(deferredTransportMessage);
        Guard.AgainstNull(deferredTransportMessageStream);

        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            await databaseContext.ExecuteAsync(_queryFactory.AddDeferredMessage(deferredTransportMessage.MessageId, processingTransportMessage.MessageId, await deferredTransportMessageStream.ToBytesAsync())).ConfigureAwait(false);
            await databaseContext.CommitTransactionAsync().ConfigureAwait(false);
        }

        return true;
    }

    public async Task DeferredMessageSentAsync(TransportMessage processingTransportMessage, TransportMessage deferredTransportMessage)
    {
        Guard.AgainstNull(processingTransportMessage);
        Guard.AgainstNull(deferredTransportMessage);

        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            await databaseContext.ExecuteAsync(_queryFactory.DeferredMessageSent(deferredTransportMessage.MessageId)).ConfigureAwait(false);
            await databaseContext.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task<IEnumerable<Stream>> GetDeferredMessagesAsync(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        var result = new List<Stream>();

        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            var rows = await databaseContext.GetRowsAsync(_queryFactory.GetDeferredMessages(transportMessage.MessageId));

            foreach (var row in rows)
            {
                result.Add(new MemoryStream((byte[])row["MessageBody"]));
            }

            await databaseContext.CommitTransactionAsync().ConfigureAwait(false);
        }

        return result;
    }

    public async Task MessageHandledAsync(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            await databaseContext.ExecuteAsync(_queryFactory.HasMessageBeenHandled(transportMessage.MessageId)).ConfigureAwait(false);
            await databaseContext.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task ProcessingCompletedAsync(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            await databaseContext.ExecuteAsync(_queryFactory.ProcessingCompleted(transportMessage.MessageId)).ConfigureAwait(false);
            await databaseContext.CommitTransactionAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask<ProcessingStatus> ProcessingStatusAsync(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        try
        {
            using (new DatabaseContextScope())
            await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
            {
                try
                {
                    if (await databaseContext.GetScalarAsync<int>(_queryFactory.HasMessageBeenCompleted(transportMessage.MessageId)).ConfigureAwait(false) == 1
                        ||
                        await databaseContext.GetScalarAsync<int>(_queryFactory.IsProcessing(transportMessage.MessageId)).ConfigureAwait(false) == 1)
                    {
                        return ProcessingStatus.Ignore;
                    }

                    await databaseContext.ExecuteAsync(_queryFactory.Processing(transportMessage.MessageId, transportMessage.RecipientInboxWorkQueueUri, Environment.CurrentManagedThreadId)).ConfigureAwait(false);

                    return await databaseContext.GetScalarAsync<int>(_queryFactory.HasMessageBeenHandled(transportMessage.MessageId)) == 1
                        ? ProcessingStatus.MessageHandled
                        : ProcessingStatus.Assigned;
                }
                finally
                {
                    await databaseContext.CommitTransactionAsync();
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