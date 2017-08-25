using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;

using Rust;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

// TODO: Custom loot tables.
// TODO: Change delay time before crates can be accesed.
// TODO: Handle Bradley respawn length and despawn times.
namespace Oxide.Plugins
{
	[Info("BradleyControl", "Mattparks", "0.1.1", ResourceId = 2611)]
	[Description("A plugin that controls Bradley properties.")]
	class BradleyControl : RustPlugin 
	{
		#region Fields
	   
		[PluginReference] Plugin Vanish;

		private List<BradleyAPC> apcs = new List<BradleyAPC>();
		private int lastFrame = Time.frameCount;
		
		#endregion

		#region Configuration

		public class Options
		{	 
			public bool bradleyEnabled = true;
			public float respawnDelay = 600.0f;
			public float despawnDelay = 600.0f;
			public float startHealth = 500.0f;
			public float maxTurretRange = 100.0f;
			public float gunAccuracy = 1.0f;
			public float speed = 1.0f;
			public int maxLootCrates = 3;
			public bool enableNapalm = true;
			public float lootAccessDelay = 120.0f;
		}

		public class ConfigData
		{
			public Options options = new Options();
		}

		private ConfigData configs;

		private void LoadDefaultConfig()
		{
			Puts("Creating a new config file!"); 
			configs = new ConfigData();
			SaveConfig();
		}

		private void LoadVariables()
		{
			configs = Config.ReadObject<ConfigData>();
			
			if (configs == null)
			{
				LoadDefaultConfig();
			}
			
			SaveConfig(); 
		}

		private void SaveConfig()
		{
			Config.WriteObject(configs, true);
		}

		#endregion

        #region Messages/Localization

		private void LoadDefaultMessages() 
		{
			// English messages.
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["BRADLEY_ABOUT"] = "<color=#ff3b3b>Bradley Control {Version}</color>: by <color=green>mattparks</color>. Bradley Control is a plugin that controls Bradley properties. Use the /bradley command as follows: \n <color=#1586db>•</color> /bradley - Displays Bradley Control about and help. \n <color=#1586db>•</color> /bradley reset - Resets the Bradley in the arena.",
				["BRADLEY_RESET"] = "<color=#ff3b3b>Resetting the Bradley!</color>",
				["BRADLEY_RESET_FAIL"] = "<color=#ff3b3b>Failed to reset the Bradley!</color>",
			}, this, "en");
		}
		
        #endregion

        #region Hooks

		private void OnServerInitialized()
		{
		//	BradleySpawner.singleton = null;
			
			LoadVariables();
			
			var allEntities = UnityEngine.Object.FindObjectsOfType<BaseEntity>();
			
			foreach (var entity in allEntities)
			{
				var prefabname = entity.name;

				if (prefabname.Contains("bradleyapc") && !prefabname.Contains("gibs")) 
				{
					UpdateBradley(entity?.GetComponent<BradleyAPC>());
				}
			}
		}

		private void OnTick()
		{
			// TODO: This kinda makes vanish around APCs not as bad...
			if (Time.frameCount - lastFrame > 10)
			{
				foreach (var apc in apcs)
				{
					foreach (var target in apc.targetList)
					{
						if (target.entity is BasePlayer)
						{
							var canNetwork = Vanish?.Call("IsInvisible", target.entity);
							
							if (canNetwork is bool)
							{
								if ((bool) canNetwork)
								{
									target.lastSeenTime = -1.0f;
									target.entity = null;
								}
							}
						}
					}
				}
				
				lastFrame = Time.frameCount;
			}
		}
		
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var prefabname = entity.name;

            if (prefabname.Contains("bradleyapc") && !prefabname.Contains("gibs"))
            {
				UpdateBradley(entity as BradleyAPC);
			}
			
			if (prefabname.Contains("oilfireball"))
            {
                var fireball = entity?.GetComponent<FireBall>() ?? null;
                
				if (fireball == null)
				{
					return;
				}
                
				if (!configs.options.enableNapalm)
                {
                    fireball.enableSaving = false;
                    NextTick(() => 
					{ 
						if (!(entity?.IsDestroyed ?? true)) 
						{
							entity.Kill(); 
						}
					});
                }
            }
		}
		
		private void OnEntityKill(BaseNetworkable entity)
		{
            var prefabname = entity.name;

            if (prefabname.Contains("bradleyapc") && !prefabname.Contains("gibs"))
            {
				var apc = entity as BradleyAPC;
				
				if (apcs.Contains(apc))
				{
					Puts(entity.name);
					apcs.Remove(apc);
				}
			}
		}

        #endregion
		
		#region Chat/Console Commands

		[ChatCommand("bradley")]
		private void CommandBradley(BasePlayer player, string command, string[] args)
		{
			if (!player.IsAdmin)
			{
				MessagePlayer(Lang("No Permission", player), player);
                return;
			}
			
			if (args.Length == 0)
			{
				MessagePlayer(Lang("BRADLEY_ABOUT", player).Replace("{Version}", Version.ToString()), player);
			}
			else
			{
				if (args[0].Equals("reset"))
				{
					if (configs.options.bradleyEnabled)
					{
						MessagePlayer(Lang("BRADLEY_RESET", player), player);
						
						BradleySpawner singleton = BradleySpawner.singleton;
						
						if (singleton == null)
						{
							Puts("No Bradley Spawner!");
						}
						else
						{
							if ((bool) singleton.spawned)
							{
								singleton.spawned.Kill(BaseNetworkable.DestroyMode.None);
							}
							
							singleton.spawned = null;
							singleton.DoRespawn();
						}
					}
					else
					{
						MessagePlayer(Lang("BRADLEY_RESPAWN_FAIL", player), player);
					}
				}
			}
		}
		
        #endregion
		
        #region BradleyControl

		private void UpdateBradley(BradleyAPC apc)
		{
			float healthFraction = apc.health / apc._maxHealth;
			apc._maxHealth = configs.options.startHealth;
			apc.health = healthFraction * apc._maxHealth;
			apc.viewDistance = configs.options.maxTurretRange;
			apc.searchRange = configs.options.maxTurretRange;
			apc.throttle = configs.options.speed; // TODO: Ensure Bradley speed.
			apc.leftThrottle = apc.throttle;
			apc.rightThrottle = apc.throttle;
			apc.maxCratesToSpawn = configs.options.maxLootCrates;
			
			if (!apcs.Contains(apc))
			{
				apcs.Add(apc);
			}				

			if (!configs.options.bradleyEnabled)
			{
				apc.Kill(); 
			}
		}

        private void UnlockCrate(LockedByEntCrate crate)
        {
            if (crate == null || (crate?.IsDestroyed ?? true)) 
			{
				return;
			}
            var lockingEnt = crate?.lockingEnt?.GetComponent<FireBall>() ?? null;
            
			if (lockingEnt != null && !(lockingEnt?.IsDestroyed ?? true))
            {
                lockingEnt.enableSaving = false;
                lockingEnt.Invoke("Extinguish", 30f);
            }
			
            crate.CancelInvoke("Think");
            crate.SetLocked(false);
            crate.lockingEnt = null;
        }

        #endregion

        #region Helpers

		private string Lang(string key, BasePlayer player)
		{
			return lang.GetMessage(key, this, player.UserIDString);
		}

		private void MessagePlayer(string message, BasePlayer player)
		{
			player.ChatMessage(message);
		}

        #endregion
	}
}
