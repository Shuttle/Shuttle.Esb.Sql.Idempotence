using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Esb.Idempotence;

namespace Shuttle.Esb.Sql.Idempotence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlIdempotence(this IServiceCollection services, Action<SqlIdempotenceBuilder>? builder = null)
    {
        var sqlIdempotenceBuilder = new SqlIdempotenceBuilder(Guard.AgainstNull(services));

        builder?.Invoke(sqlIdempotenceBuilder);

        services.AddSingleton(Options.Create(sqlIdempotenceBuilder.Options));

        return services
            .AddSingleton<IValidateOptions<SqlIdempotenceOptions>, SqlIdempotenceOptionsValidator>()
            .AddSingleton<IdempotenceObserver>()
            .AddSingleton<IIdempotenceService, IdempotenceService>()
            .AddSingleton<IHostedService, IdempotenceHostedService>();
    }
}