using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlIdempotence(this IServiceCollection services,
            Action<IdempotenceBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var idempotenceBuilder = new IdempotenceBuilder(services);

            builder?.Invoke(idempotenceBuilder);

            services.TryAddSingleton<IScriptProvider, ScriptProvider>();
            services.AddSingleton<IIdempotenceService, IdempotenceService>();

            services.AddOptions<IdempotenceOptions>().Configure(options =>
            {
                options.ConnectionStringName = idempotenceBuilder.Options.ConnectionStringName;
            });

            return services;
        }
    }
}