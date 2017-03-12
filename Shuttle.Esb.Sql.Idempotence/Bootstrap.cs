﻿using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Sql.Idempotence
{
	public class Bootstrap : IComponentRegistryBootstrap
	{
		public void Register(IComponentRegistry registry)
		{
			Guard.AgainstNull(registry, "registry");

			registry.AttemptRegister<IScriptProviderConfiguration, ScriptProviderConfiguration>();
			registry.AttemptRegister<IScriptProvider, ScriptProvider>();
		}
	}
}