using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlIdempotence(this IServiceCollection services, Action<SqlIdempotenceBuilder>? builder = null)
    {
        var sqlIdempotenceBuilder = new SqlIdempotenceBuilder(Guard.AgainstNull(services));

        builder?.Invoke(sqlIdempotenceBuilder);

        services.AddSingleton<IValidateOptions<SqlIdempotenceOptions>, SqlIdempotenceOptionsValidator>();

        services.AddOptions<SqlIdempotenceOptions>().Configure(options =>
        {
            options.ConnectionStringName = sqlIdempotenceBuilder.Options.ConnectionStringName;
            options.Schema = sqlIdempotenceBuilder.Options.Schema;
        });

        services.AddSingleton<IIdempotenceService, IdempotenceService>();
        services.AddSingleton<IdempotenceObserver>();
        services.AddSingleton<IHostedService, IdempotenceHostedService>();

        return services;
    }
}