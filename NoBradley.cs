using System;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
	[Info("NoBradley", "Mattparks", "0.1.0", ResourceId = 0)]
	[Description("Simply stops the bradley from ever spawning.")]
	class NoBradley : RustPlugin 
	{
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var prefabname = entity?.ShortPrefabName ?? string.Empty;

            if (prefabname.Contains("bradleyapc") && !prefabname.Contains("gibs"))
            {
				NextTick(() => 
				{ 
					if (!(entity?.IsDestroyed ?? true)) 
					{
						Puts("Killing Bradley APC!");
						entity.Kill(); 
					}
				});
			}
		}
	}
}
