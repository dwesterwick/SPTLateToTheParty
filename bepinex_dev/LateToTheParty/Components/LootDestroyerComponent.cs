using System;
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
    public class LootDestroyerComponent : MonoBehaviour
    {
        public LootManager LootManager { get; } = new LootManager();

        private Stopwatch lootDestructionTimer = Stopwatch.StartNew();
        private Stopwatch updateTimer = Stopwatch.StartNew();

        protected void Awake()
        {
            LootManager.FindAllLootableContainers();
        }

        protected void Update()
        {
            // Skip the frame if the coroutine from the previous frame(s) are still running
            if (LootManager.IsFindingAndDestroyingLoot)
            {
                return;
            }

            // Skip the frame if not enough time has elapsed
            if (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.MinTimeBeforeUpdate)
            {
                return;
            }

            // If the setting is enabled, only allow loot to be destroyed for a certain time after spawning
            if (shouldLimitEvents())
            {
                return;
            }

            // Wait until doors have been opened to ensure loot will be destroyed behind previously locked doors
            if (!Singleton<DoorTogglingComponent>.Instance.HasToggledInitialInteractiveObjects)
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
            float maxDistanceTravelledByPlayers = Singleton<PlayerMonitor>.Instance.GetMostDistanceTravelledByPlayer();
            if (
                LootManager.HasInitialLootBeenDestroyed
                && (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.MaxTimeBeforeUpdate)
                && (maxDistanceTravelledByPlayers < ConfigController.Config.DestroyLootDuringRaid.MinDistanceTraveledForUpdate)
            )
            {
                return;
            }

            // Spread the work out across multiple frames to avoid stuttering
            IEnumerable<Vector3> alivePlayerPositions = Singleton<PlayerMonitor>.Instance.GetPlayerPositions();
            StartCoroutine(LootManager.FindAndDestroyLoot(alivePlayerPositions, timeRemainingFraction, raidTimeElapsed));
            updateTimer.Restart();
        }

        private bool shouldLimitEvents()
        {
            return ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.LootDestruction
                && LootManager.HasInitialLootBeenDestroyed
                && (lootDestructionTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);
        }
    }
}
