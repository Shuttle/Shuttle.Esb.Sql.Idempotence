using Shuttle.Core.Container;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence
{
	public static class ComponentRegistryExtensions
	{
		public static void RegisterIdempotence(this IComponentRegistry registry)
		{
			Guard.AgainstNull(registry, "registry");

			registry.AttemptRegister<IScriptProviderConfiguration, ScriptProviderConfiguration>();
			registry.AttemptRegister<IScriptProvider, ScriptProvider>();

		    if (!registry.IsRegistered<IIdempotenceConfiguration>())
		    {
		        registry.RegisterInstance<IIdempotenceConfiguration>(IdempotenceSection.Configuration());
		    }

		    registry.AttemptRegister<IIdempotenceService, IdempotenceService>();
		}
	}
}