using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using EFT.UI;
using System.ComponentModel;
using System.Collections;
using EFT.Game.Spawning;
using LateToTheParty.Models;

namespace LateToTheParty.Controllers
{
    public class LootDestroyerController : MonoBehaviour
    {
        private static Vector3 lastUpdatePosition = Vector3.zero;        
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void Update()
        {
            if (!LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.Enabled)
            {
                return;
            }

            // Skip the frame if the coroutine from the previous frame(s) are still running or not enough time has elapsed
            if (LootManager.IsFindingAndDestroyingLoot || (updateTimer.ElapsedMilliseconds < LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.MinTimeBeforeUpdate))
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                LootManager.ItemsDroppedByMainPlayer.Clear();
                LootManager.AllLootableContainers.Clear();
                LootManager.LootInfo.Clear();

                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1426.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (Patches.ReadyToPlayPatch.LastOriginalEscapeTime * 60f) - escapeTimeSec;
            float timeRemainingFraction = escapeTimeSec / (Patches.ReadyToPlayPatch.LastOriginalEscapeTime * 60f);
            if ((escapeTimeSec > 3600 * 24 * 90) || (timeRemainingFraction > 0.995))
            {
                return;
            }

            // Only run the script if you've traveled a minimum distance from the last update. Othewise, stuttering will occur. 
            // However, ignore this check initially so loot can be despawned at the very beginning of the raid before you start moving if you spawn in late
            Vector3 yourPosition = Camera.main.transform.position;
            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if ((updateTimer.ElapsedMilliseconds < LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.MaxTimeBeforeUpdate) && (lastUpdateDist < LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.MinDistanceTraveledForUpdate) && (LootManager.LootInfo.Count > 0))
            {
                return;
            }

            // This should only be run once to generate the list of lootable containers in the map
            if (LootManager.AllLootableContainers.Count == 0)
            {
                LateToThePartyPlugin.Log.LogInfo("Searching for lootable containers in the map...");
                LootManager.AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
                LateToThePartyPlugin.Log.LogInfo("Searching for lootable containers in the map...found " + LootManager.AllLootableContainers.Count + " lootable containers.");
            }

            // Spread the work out across multiple frames to avoid stuttering
            StartCoroutine(LootManager.FindAndDestroyLoot(yourPosition, timeRemainingFraction, raidTimeElapsed));
            updateTimer.Restart();
        }
    }
}
