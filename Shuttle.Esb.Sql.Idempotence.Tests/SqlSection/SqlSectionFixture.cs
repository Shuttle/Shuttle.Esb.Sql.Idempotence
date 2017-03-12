using System;
using System.IO;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Idempotence.Tests
{
	public class SqlSectionFixture
	{
		protected IdempotenceSection GetSqlSection(string file)
		{
			return ConfigurationSectionProvider.OpenFile<IdempotenceSection>("shuttle", "sqlServer", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format(@".\IdempotenceSection\files\{0}", file)));
		}
	}
}