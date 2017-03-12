using Castle.Windsor;
using NUnit.Framework;
using Shuttle.Core.Castle;
using Shuttle.Core.Infrastructure;
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

			TestIdempotenceProcessing(new ComponentContainer(container, () => container), @"sql://shuttle/{0}", isTransactionalEndpoint,
                enqueueUniqueMessages);
        }
    }
}