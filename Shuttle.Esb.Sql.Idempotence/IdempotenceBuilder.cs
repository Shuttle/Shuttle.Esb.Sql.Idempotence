using System;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence
{
    public class IdempotenceBuilder
    {
        private IdempotenceOptions _idempotenceOptions = new IdempotenceOptions();
        public IServiceCollection Services { get; }

        public IdempotenceBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));

            Services = services;
        }

        public IdempotenceOptions Options
        {
            get => _idempotenceOptions;
            set => _idempotenceOptions = value ?? throw new ArgumentNullException(nameof(value));
        }

    }
}