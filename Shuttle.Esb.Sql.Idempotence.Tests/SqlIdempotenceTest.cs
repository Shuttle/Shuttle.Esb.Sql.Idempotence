using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
    [TestFixture]
    public class SqlIdempotenceTest : IdempotenceFixture
    {
        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint, bool enqueueUniqueMessages)
        {
            TestIdempotenceProcessing(SqlFixture.GetComponentContainer(), @"sql://shuttle/{0}", isTransactionalEndpoint,
                enqueueUniqueMessages);
        }
    }
}