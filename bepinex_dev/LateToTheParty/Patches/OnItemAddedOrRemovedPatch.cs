using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using LateToTheParty.Controllers;
using LateToTheParty.Models;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Item item, ItemAddress location, bool added)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
            {
                return;
            }

            // If you pick up an item, it needs to be removed from the loot lists to prevent it from being randomly despawned while in your inventory
            if (added)
            {
                LootManager.RegisterItemPickedUpByPlayer(item);
                return;
            }

            if (!LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.Enabled)
            {
                Logger.LogInfo("Ignoring item removed by player: " + item.LocalizedName());
                return;
            }

            if (LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.OnlyItemsBroughtIntoRaid && item.SpawnedInSession)
            {
                Logger.LogInfo("Ignoring not-FIR item removed by player: " + item.LocalizedName());
                return;
            }

            LootManager.RegisterItemDroppedByPlayer(item);
        }
    }
}
