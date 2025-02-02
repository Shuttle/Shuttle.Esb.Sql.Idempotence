using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence;

public class SqlIdempotenceBuilder
{
    private SqlIdempotenceOptions _sqlIdempotenceOptions = new();

    public SqlIdempotenceBuilder(IServiceCollection services)
    {
        Services = Guard.AgainstNull(services);
    }

    public SqlIdempotenceOptions Options
    {
        get => _sqlIdempotenceOptions;
        set => _sqlIdempotenceOptions = Guard.AgainstNull(value);
    }

    public IServiceCollection Services { get; }

    public SqlIdempotenceBuilder UseSqlServer()
    {
        Services.AddSingleton<IQueryFactory, SqlServer.QueryFactory>();

        return this;
    }
}