using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
	public class IdempotenceOptionsFixture
	{
		protected IdempotenceOptions GetOptions()
		{
			var result = new IdempotenceOptions();

			new ConfigurationBuilder()
				.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\appsettings.json")).Build()
				.GetRequiredSection($"{IdempotenceOptions.SectionName}").Bind(result);

			return result;
		}

		[Test]
		public void Should_be_able_to_get_full_section()
		{
			var options = GetOptions();

			Assert.IsNotNull(options);

			Assert.AreEqual("connection-string-name", options.ConnectionStringName);
		}
	}
}