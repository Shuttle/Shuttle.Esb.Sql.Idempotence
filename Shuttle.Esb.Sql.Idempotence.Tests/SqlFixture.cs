using Castle.Windsor;
using Shuttle.Core.Castle;
using Shuttle.Core.Data;
using Shuttle.Core.Infrastructure;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
    public static class SqlFixture
    {
        public static ComponentContainer GetComponentContainer()
        {
            return GetComponentContainer(true);
        }

        public static ComponentContainer GetComponentContainer(bool registerIdempotenceService)
        {
            var container = new WindsorComponentContainer(new WindsorContainer());

            if (registerIdempotenceService)
            {
                container.Register<IIdempotenceService, IdempotenceService>();
            }

            return new ComponentContainer(container, () => container);
        }
    }
}