using System;
using System.Configuration;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Idempotence
{
	public class IdempotenceSection : ConfigurationSection
	{
		[ConfigurationProperty("idempotenceServiceConnectionStringName", IsRequired = false,
			DefaultValue = "Idempotence")]
		public string IdempotenceServiceConnectionStringName
		{
			get { return (string) this["idempotenceServiceConnectionStringName"]; }
		}

		public static IdempotenceConfiguration Configuration()
		{
			var section = ConfigurationSectionProvider.Open<IdempotenceSection>("shuttle", "sql");
			var configuration = new IdempotenceConfiguration();

			var subscriptionManagerConnectionStringName = "Subscription";
			var idempotenceServiceConnectionStringName = "Idempotence";

			if (section != null)
			{
				subscriptionManagerConnectionStringName = section.SubscriptionManagerConnectionStringName;
				idempotenceServiceConnectionStringName = section.IdempotenceServiceConnectionStringName;
				configuration.IgnoreSubscribe = section.IgnoreSubscribe;
			}

			configuration.SubscriptionManagerProviderName = GetSettings(subscriptionManagerConnectionStringName).ProviderName;
			configuration.SubscriptionManagerConnectionString = GetSettings(subscriptionManagerConnectionStringName).ConnectionString;
			configuration.IdempotenceServiceProviderName = GetSettings(idempotenceServiceConnectionStringName).ProviderName;
			configuration.IdempotenceServiceConnectionString = GetSettings(idempotenceServiceConnectionStringName).ConnectionString;

			return configuration;
		}

		private static ConnectionStringSettings GetSettings(string connectionStringName)
		{
			var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

			if (settings == null)
			{
				throw new InvalidOperationException(string.Format(SqlResources.ConnectionStringMissing, connectionStringName));
			}

			return settings;
		}
	}
}