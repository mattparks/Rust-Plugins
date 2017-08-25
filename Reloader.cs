using System;
using System.Collections.Generic;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("Reloader", "Mattparks", "0.1.0", ResourceId = 0)]
	[Description("Gives the ability to reload oxide plugins to a usergroup.")]
	class Reloader : RustPlugin 
	{
        private const string permissionName = "reloader.use";

        #region Configuration

		private void LoadDefaultConfig()
		{
			SaveConfig();
		}

        #endregion

        #region Messages/Localization

		private void LoadDefaultMessages() 
		{
			// English messages.
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["RELOADER_ABOUT"] = "<color=red>Reloader " + Version + "</color>: by <color=green>mattparks</color>. Reloader is a plugin that gives the ability to reload oxide plugins to a usergroup. Use the /reloader command as follows: \n /reloader reload *|<plugin>+ - Reloads a plugin.",
				["RELOADER_COMPLETE"] = "<color=red>Reloader</color>: Reloading plugin: ",
				["RELOADER_FAILED"] = "<color=red>Reloader</color>: Filed to reload plugin: ",
				["RELOADER_ALL"] = "<color=red>Reloader</color>: Reloading all plugins",
			}, this, "en");
		}

        #endregion

        #region Initialization

		private void Init()
		{
			LoadDefaultConfig();
			LoadDefaultMessages();

			// Registers permissions.
			permission.RegisterPermission(permissionName, this);
		}

		#endregion

        #region Chat/Console Commands

		[ChatCommand("reloader")]
		private void ReloaderCmd(BasePlayer player, string command, string[] args)
		{
			if (!permission.UserHasPermission(player.UserIDString, permissionName))
			{
				MessagePlayer(Lang("No Permission", player), player);
                return;
			}

			if (args.Length > 0)
			{
				if (args[0].Equals("*") || args[0].Equals("all"))
				{
					MessagePlayer(Lang("RELOADER_ALL", player), player);
					Interface.Oxide.ReloadAllPlugins();
					return;
				}

				foreach (var name in args)
				{
					if (!string.IsNullOrEmpty(name)) 
					{
						if (Interface.Oxide.ReloadPlugin(name))
						{
							MessagePlayer(Lang("RELOADER_COMPLETE", player) + name, player);
						}
						else
						{
							MessagePlayer(Lang("RELOADER_FAILED", player) + name, player);
						}
					}
				}
			}
			else
			{
				MessagePlayer(Lang("RELOADER_ABOUT", player), player);
			}
		}
		
        #endregion
	}
}
