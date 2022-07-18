using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
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
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            var services = new ServiceCollection();

            services.AddDataAccess(builder =>
            {
                builder.AddConnectionString("shuttle", "System.Data.SqlClient",
                    "server=.;database=shuttle;user id=sa;password=Pass!000");
            });

            services.AddSqlQueue();
            services.AddSqlIdempotence(builder =>
            {
                builder.Options.ConnectionStringName = "shuttle";
            });

            TestIdempotenceProcessing(services, @"sql://shuttle/{0}",  isTransactionalEndpoint, enqueueUniqueMessages);
        }
    }
}