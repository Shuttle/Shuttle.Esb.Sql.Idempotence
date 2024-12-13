using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;
using Shuttle.Esb.Idempotence;

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

    public async Task RegisterAsync(TransportMessage transportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName))
        {
            await databaseContext.ExecuteAsync(_queryFactory.Register(Guard.AgainstNull(transportMessage))).ConfigureAwait(false);
        }
    }

    public async Task HandledAsync(TransportMessage transportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName))
        {
            await databaseContext.ExecuteAsync(_queryFactory.Handled(Guard.AgainstNull(transportMessage))).ConfigureAwait(false);
        }
    }

    public async ValueTask<bool> ContainsAsync(TransportMessage transportMessage)
    {
        using (new DatabaseContextScope())
        await using (var databaseContext = _databaseContextFactory.Create(_sqlIdempotenceOptions.ConnectionStringName))
        {
            return await databaseContext.GetScalarAsync<int>(_queryFactory.Contains(Guard.AgainstNull(transportMessage))).ConfigureAwait(false) == 1;
        }
    }
}