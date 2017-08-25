using System;
using System.Collections.Generic;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("FurnaceDivider", "Mattparks", "0.1.0", ResourceId = 0)]
	[Description("Makes dividing ores in furnaces easier.")]
	class FurnaceDivider : RustPlugin 
	{
        private readonly static string[] compatibleOvens =
        {
            "furnace",
            "furnace.large",
            "campfire",
            "refinery_small_deployed"
        };
        
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
				["FURNACE_DIVIDER_ABOUT"] = "<color=red>FurnaceDivider " + Version + "</color>: by <color=green>mattparks</color>. FurnaceDivider is a plugin that divides ores in furnaces.",
			}, this, "en");
		}

        #endregion

        #region Initialization

		private void Init()
		{
			LoadDefaultConfig();
			LoadDefaultMessages();
		}

		#endregion

        #region Chat/Console Commands

		[ChatCommand("furnace")]
		private void FurnaceCmd(BasePlayer player, string command, string[] args)
		{
			MessagePlayer(Lang("FURNACE_DIVIDER_ABOUT", player), player);
		}
		
        #endregion

        #region OnConsumeFuel

		void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
		{
			Dictionary<ItemDefinition, int> contained = new Dictionary<ItemDefinition, int>();
			int oreCount = 0;

			foreach (var i in oven.inventory.itemList)
			{
				if (!contained.ContainsKey(i.info))
				{
					contained.Add(i.info, 0);
				}

				int value;
				contained.TryGetValue(i.info, out value);
				value += i.amount;
				
				if (i.info.shortname.Contains("ore"))
				{
					oreCount++;
				}

				contained.Remove(i.info);
				contained.Add(i.info, value);
			}
			
			oven.inventory.itemList.Clear();
			
			// Slot 0, wood.
			// Slot 1, charcoal.
			// If ore size > 2 : ignore.
			// If ore size == 2, ore 1 slot 3, ore 2 slot 4.
			// If ore size == 1, ore/3 into 3 4 5.
			
			Puts("Ore count: " + oreCount);
				
			foreach(var entry in contained)
			{
				Item item = ItemManager.CreateByItemID(entry.Key.itemid, entry.Value);
				oven.inventory.itemList.Add(item);
				var size = entry.Value;
				Puts("" + entry.ToString());
			}
		}

        #endregion

        #region Helpers

		private T GetConfig<T>(string name, T original)
		{
			// Returns the reading of present.
			if (Config[name] != null)
			{
				return (T)Convert.ChangeType(Config[name], typeof(T));
			}

			// Otherwise return the original.
			return original;
		}
		
		private string Lang(string key, BasePlayer player)
		{
			return lang.GetMessage(key, this, player.UserIDString);
		}

		private void MessagePlayer(string message, BasePlayer player)
		{
			player.ChatMessage(message);
		}

        #endregion
		
		#region Behaviours

        #endregion
	}
}
