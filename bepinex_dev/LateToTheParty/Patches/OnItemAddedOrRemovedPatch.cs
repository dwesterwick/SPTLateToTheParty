using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(Player __instance, Item item, ItemAddress location, bool added)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            // If a player picked up an item, it needs to be tracked to prevent it from being randomly despawned while in the player's inventory
            if (added)
            {
                LootManager.RegisterItemPickedUpByPlayer(item);
                return;
            }

            bool preventFromDespawning = false;

            if (PlayerMonitorController.GetPlayerIDs().Contains(__instance.Profile.Id))
            {
                if (ConfigController.Config.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.Enabled)
                {
                    preventFromDespawning = true;
                }

                if (ConfigController.Config.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.OnlyItemsBroughtIntoRaid && item.SpawnedInSession)
                {
                    preventFromDespawning = false;
                }
            }

            LootManager.RegisterItemDroppedByPlayer(item, preventFromDespawning);
        }
    }
}
