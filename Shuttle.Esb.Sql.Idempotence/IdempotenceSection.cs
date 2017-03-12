using System;
using System.Configuration;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Idempotence
{
	public class IdempotenceSection : ConfigurationSection
	{
		[ConfigurationProperty("connectionStringName", IsRequired = false,
			DefaultValue = "Idempotence")]
		public string ConnectionStringName
		{
			get { return (string) this["connectionStringName"]; }
		}

		public static IdempotenceConfiguration Configuration()
		{
			var section = ConfigurationSectionProvider.Open<IdempotenceSection>("shuttle", "idempotence");
			var configuration = new IdempotenceConfiguration();

			var connectionStringName = "Idempotence";

			if (section != null)
			{
				connectionStringName = section.ConnectionStringName;
			}

			configuration.ProviderName = GetSettings(connectionStringName).ProviderName;
			configuration.ConnectionString = GetSettings(connectionStringName).ConnectionString;

			return configuration;
		}

		private static ConnectionStringSettings GetSettings(string connectionStringName)
		{
			var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

			if (settings == null)
			{
				throw new InvalidOperationException(string.Format(IdempotenceResources.ConnectionStringMissing, connectionStringName));
			}

			return settings;
		}
	}
}