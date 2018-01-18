using System;
using System.IO;
using NUnit.Framework;
using Shuttle.Core.Configuration;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
	public class SectionFixture
	{
		protected IdempotenceSection GetSection(string file)
		{
			return ConfigurationSectionProvider.OpenFile<IdempotenceSection>("shuttle", "idempotence", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@".\files\{0}", file)));
		}

		[Test]
		[TestCase("Idempotence.config")]
		[TestCase("Idempotence-Grouped.config")]
		public void Should_be_able_to_get_full_section(string file)
		{
			var section = GetSection(file);

			Assert.IsNotNull(section);

			Assert.AreEqual("connection-string-name", section.ConnectionStringName);
		}
	}
}