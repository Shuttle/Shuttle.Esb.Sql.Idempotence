using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlIdempotence(this IServiceCollection services)
    {
        Guard.AgainstNull(services, nameof(services));

        services.TryAddSingleton<IScriptProvider, ScriptProvider>();
        services.AddSingleton<IIdempotenceService, IdempotenceService>();

        return services;
    }
}