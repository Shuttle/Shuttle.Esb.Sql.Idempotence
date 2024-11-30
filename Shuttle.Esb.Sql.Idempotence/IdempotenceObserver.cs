using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Core.Pipelines;
using System;
using System.Threading.Tasks;

namespace Shuttle.Esb.Sql.Idempotence;

public class IdempotenceObserver : IPipelineObserver<OnStarted>
{
    private readonly IServiceBusConfiguration _serviceBusConfiguration;
    private readonly SqlIdempotenceOptions _sqlIdempotenceOptions;
    private readonly IQueryFactory _queryFactory;
    private readonly IDatabaseContextFactory _databaseContextFactory;

    public IdempotenceObserver(IOptions<SqlIdempotenceOptions> sqlIdempotenceOptions, IServiceBusConfiguration serviceBusConfiguration, IQueryFactory queryFactory, IDatabaseContextFactory databaseContextFactory)
    {
        _sqlIdempotenceOptions = Guard.AgainstNull(Guard.AgainstNull(sqlIdempotenceOptions).Value);
        _serviceBusConfiguration = Guard.AgainstNull(serviceBusConfiguration);

        if (serviceBusConfiguration.Inbox?.WorkQueue == null)
        {
            throw new InvalidOperationException(Resources.NoInboxException);
        }

        _queryFactory = Guard.AgainstNull(queryFactory);
        _databaseContextFactory = Guard.AgainstNull(databaseContextFactory);
    }

    public async Task ExecuteAsync(IPipelineContext<OnStarted> pipelineContext)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = await _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName).BeginTransactionAsync())
        {
            await databaseContext.ExecuteAsync(_queryFactory.Create());
            await databaseContext.ExecuteAsync(_queryFactory.Initialize(_serviceBusConfiguration.Inbox!.WorkQueue!.Uri.ToString()));
            await databaseContext.CommitTransactionAsync();
        }
    }
}