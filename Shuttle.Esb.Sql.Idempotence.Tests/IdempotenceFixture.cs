﻿using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Core.Data;
using Shuttle.Esb.Idempotence;

namespace Shuttle.Esb.Sql.Idempotence.Tests;

[TestFixture]
public class IdempotenceFixture : Esb.Idempotence.Tests.IdempotenceFixture
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
    public async Task Should_be_able_to_perform_full_processing_async(bool isTransactionalEndpoint, bool enqueueUniqueMessages)
    {
        await TestIdempotenceProcessingAsync(GetServiceCollection(), isTransactionalEndpoint, enqueueUniqueMessages);
    }

    private static IServiceCollection GetServiceCollection()
    {
        return new ServiceCollection()
            .AddDataAccess(builder =>
            {
                builder.AddConnectionString("Idempotence", "Microsoft.Data.SqlClient",
                    "server=.;database=shuttle;user id=sa;password=Pass!000;TrustServerCertificate=true");
            })
            .AddSqlIdempotence(builder =>
            {
                builder.Options.Schema = "Idempotence";
                builder.Options.ConnectionStringName = "Idempotence";

                builder.UseSqlServer();
            });
    }
}