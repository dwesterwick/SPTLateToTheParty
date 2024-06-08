using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class LootDestroyerController : MonoBehaviour
    {
        private static Stopwatch lootDestructionTimer = new Stopwatch();
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void Update()
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

                return;
            }

            if (!LocationSettingsController.HasRaidStarted)
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

            float timeRemainingFraction = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            float raidTimeElapsed = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
            
            // Ensure the raid is progressing before running anything
            if (raidTimeElapsed < 10)
            {
                return;
            }

            // Only run the script if you've traveled a minimum distance from the last update. Othewise, stuttering will occur. 
            // However, ignore this check initially so loot can be despawned at the very beginning of the raid before you start moving if you spawn in late
            float maxDistanceTravelledByPlayers = Controllers.PlayerMonitorController.GetMostDistanceTravelledByPlayer();
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
                LootManager.FindAllLootableContainers(LocationSettingsController.CurrentLocation.Name);
            }

            // Wait until doors have been opened to ensure loot will be destroyed behind previously locked doors
            if (!InteractiveObjectController.HasToggledInitialInteractiveObjects)
            {
                return;
            }

            // Spread the work out across multiple frames to avoid stuttering
            IEnumerable<Vector3> alivePlayerPositions = Controllers.PlayerMonitorController.GetPlayerPositions();
            StartCoroutine(LootManager.FindAndDestroyLoot(alivePlayerPositions, timeRemainingFraction, raidTimeElapsed));
            updateTimer.Restart();
            lootDestructionTimer.Start();
        }
    }
}
