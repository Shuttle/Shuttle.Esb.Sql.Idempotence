using System.Data.Common;
using System.Data.SqlClient;
using Castle.Windsor;
using NUnit.Framework;
using Shuttle.Core.Castle;
using Shuttle.Core.Container;
using Shuttle.Core.Data;
using Shuttle.Esb.Sql.Queue;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
    [TestFixture]
    public class IdempotenceTest : IdempotenceFixture
    {
        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint, bool enqueueUniqueMessages)
        {
            var container = new WindsorComponentContainer(new WindsorContainer());

            container.RegisterInstance<IIdempotenceConfiguration>(new IdempotenceConfiguration
            {
                ProviderName = "System.Data.SqlClient",
                ConnectionString = "server=.;database=shuttle;user id=sa;password=Pass!000"
            });

            container.Register<IConnectionConfigurationProvider, ConnectionConfigurationProvider>();
            container.RegisterSqlQueue();
            container.RegisterIdempotence();
            container.RegisterDataAccess();

            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
            
            TestIdempotenceProcessing(new ComponentContainer(container, () => container), @"sql://shuttle/{0}",
                isTransactionalEndpoint, enqueueUniqueMessages);
        }
    }
}