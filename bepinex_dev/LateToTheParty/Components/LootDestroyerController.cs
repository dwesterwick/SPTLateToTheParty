﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Components
{
    public class LootDestroyerController : MonoBehaviour
    {
        private static Stopwatch lootDestructionTimer = new Stopwatch();
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static bool canDestroyLoot = true;

        protected void Update()
        {
            if (LootManager.IsClearing)
            {
                return;
            }

            if (!ConfigController.Config.DestroyLootDuringRaid.Enabled)
            {
                return;
            }

            // Skip the frame if the coroutine from the previous frame(s) are still running or not enough time has elapsed
            if (LootManager.IsFindingAndDestroyingLoot || (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.MinTimeBeforeUpdate))
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(LootManager.Clear());
                lootDestructionTimer.Reset();
                canDestroyLoot = true;

                return;
            }

            if (!LocationSettingsController.HasRaidStarted || !canDestroyLoot)
            {
                return;
            }

            // If the setting is enabled, only allow loot to be destroyed for a certain time after spawning
            if
            (
                ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.LootDestruction
                && LootManager.HasInitialLootBeenDestroyed
                && (lootDestructionTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit)
            )
            {
                return;
            }

            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            //if (!Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return;
            }

            float timeRemainingFraction = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            float raidTimeElapsed = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
            
            // Ensure the raid is progressing before running anything
            if (raidTimeElapsed < 10)
            {
                return;
            }

            // Only run the script if you've traveled a minimum distance from the last update. Othewise, stuttering will occur. 
            // However, ignore this check initially so loot can be despawned at the very beginning of the raid before you start moving if you spawn in late
            float maxDistanceTravelledByPlayers = PlayerMonitor.GetMostDistanceTravelledByPlayer();
            if (
                (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.MaxTimeBeforeUpdate)
                && (maxDistanceTravelledByPlayers < ConfigController.Config.DestroyLootDuringRaid.MinDistanceTraveledForUpdate)
                && (LootManager.TotalLootItemsCount > 0)
            )
            {
                return;
            }

            // This should only be run once to generate the list of lootable containers in the map
            if (LootManager.LootableContainerCount == 0)
            {
                // Only enable the following line for testing
                //Controllers.LocationSettingsController.SetCurrentLocation(null);

                // If CurrentLocation is null, OnGameStartedPatch did not run (likely due to this mod not working with Fika) 
                if (LocationSettingsController.CurrentLocation == null)
                {
                    LoggingController.LogErrorToServerConsole("Cannot determine the current map. Disabling loot destruction.");
                    canDestroyLoot = false;
                    return;
                }

                LootManager.FindAllLootableContainers(LocationSettingsController.CurrentLocation.Name);
            }

            // Wait until doors have been opened to ensure loot will be destroyed behind previously locked doors
            if (!InteractiveObjectController.HasToggledInitialInteractiveObjects)
            {
                return;
            }

            // Spread the work out across multiple frames to avoid stuttering
            IEnumerable<Vector3> alivePlayerPositions = PlayerMonitor.GetPlayerPositions();
            StartCoroutine(LootManager.FindAndDestroyLoot(alivePlayerPositions, timeRemainingFraction, raidTimeElapsed));
            updateTimer.Restart();
            lootDestructionTimer.Start();
        }
    }
}
