using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Comfort.Common;
using EFT;
using LateToTheParty.Models;

namespace LateToTheParty.Controllers
{
    public class LootDestroyerController : MonoBehaviour
    {
        private static Vector3 lastUpdatePosition = Vector3.zero;        
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static int foundLootableContainers = -1;

        private void Update()
        {
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
                LootManager.Clear();
                foundLootableContainers = -1;

                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1425.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (LocationSettingsController.LastOriginalEscapeTime * 60f) - escapeTimeSec;
            float timeRemainingFraction = escapeTimeSec / (LocationSettingsController.LastOriginalEscapeTime * 60f);

            // Ensure the raid is progressing before running anything
            if (raidTimeElapsed < 10)
            {
                return;
            }

            // Only run the script if you've traveled a minimum distance from the last update. Othewise, stuttering will occur. 
            // However, ignore this check initially so loot can be despawned at the very beginning of the raid before you start moving if you spawn in late
            Vector3 yourPosition = Camera.main.transform.position;
            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if (
                (updateTimer.ElapsedMilliseconds < ConfigController.Config.DestroyLootDuringRaid.MaxTimeBeforeUpdate)
                && (lastUpdateDist < ConfigController.Config.DestroyLootDuringRaid.MinDistanceTraveledForUpdate)
                && (LootManager.TotalLootItemsCount > 0)
            )
            {
                return;
            }

            // This should only be run once to generate the list of lootable containers in the map
            if (foundLootableContainers == -1)
            {
                foundLootableContainers = LootManager.FindAllLootableContainers();
            }

            // Ensure there are loot containers on the map
            if (foundLootableContainers == 0)
            {
                return;
            }

            // Spread the work out across multiple frames to avoid stuttering
            StartCoroutine(LootManager.FindAndDestroyLoot(yourPosition, timeRemainingFraction, raidTimeElapsed));
            lastUpdatePosition = yourPosition;
            updateTimer.Restart();
        }
    }
}
