using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;
using LateToTheParty.Components;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class OnBeenKilledByAggressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnBeenKilledByAggressor", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(Player __instance, Player aggressor)
        {
            if (!ConfigController.Config.DestroyLootDuringRaid.IgnoreItemsOnDeadBots.Enabled)
            {
                LoggingController.LogInfo("Player " + __instance.Profile.Nickname + " was killed by " + aggressor.Profile.Nickname + " (allowing their loot to despawn)");
                return;
            }

            if
            (
                ConfigController.Config.DestroyLootDuringRaid.IgnoreItemsOnDeadBots.OnlyIfYouKilledThem
                && !Singleton<PlayerMonitor>.Instance.GetPlayerIDs().Contains(aggressor.Profile.Id)
            )
            {
                LoggingController.LogInfo("Player " + __instance.Profile.Nickname + " was killed by " + aggressor.Profile.Nickname + " (allowing their loot to despawn because a human player didn't kill them)");
                return;
            }

            // Prevent all items in the player's inventory from despawning
            IEnumerable<Item> allPlayerItems = __instance.Profile.Inventory.Equipment.GetAllItems();
            foreach (Item item in allPlayerItems)
            {
                Singleton<LootDestroyerComponent>.Instance.LootManager.RegisterItemDroppedByPlayer(item, true);
            }
        }
    }
}
