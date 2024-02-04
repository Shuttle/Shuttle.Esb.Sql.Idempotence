using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Esb.Sql.Queue;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.Sql.Idempotence.Tests;

[TestFixture]
public class IdempotenceTest : IdempotenceFixture
{
    [SetUp]
    public void SetUp()
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
    }

    [Test]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint, bool enqueueUniqueMessages)
    {
        TestIdempotenceProcessing(GetServiceCollection(), @"sql://idempotence/{0}", isTransactionalEndpoint, enqueueUniqueMessages);
    }

    [Test]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Should_be_able_to_perform_full_processing_async(bool isTransactionalEndpoint, bool enqueueUniqueMessages)
    {
        await TestIdempotenceProcessingAsync(GetServiceCollection(), @"sql://idempotence/{0}", isTransactionalEndpoint, enqueueUniqueMessages);
    }

    private static ServiceCollection GetServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddDataAccess(builder =>
        {
            builder.AddConnectionString("Idempotence", "Microsoft.Data.SqlClient",
                "server=.;database=shuttle;user id=sa;password=Pass!000;TrustServerCertificate=true");
        });

        services.AddSqlQueue(builder =>
        {
            builder.AddOptions("idempotence", new SqlQueueOptions
            {
                ConnectionStringName = "Idempotence"
            });
        });

        services.AddSqlIdempotence();

        return services;
    }
}