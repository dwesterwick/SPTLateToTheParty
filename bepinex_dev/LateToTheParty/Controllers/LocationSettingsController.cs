using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LocationSettingsController
    {
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;

        private static string[] CarExtractNames = new string[0];
        private static Dictionary<string, Models.LocationSettings> OriginalSettings = new Dictionary<string, Models.LocationSettings>();
        private static Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>> nearestSpawnPointPositions = new Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>>();
        
        public static void ClearOriginalSettings()
        {
            LoggingController.LogInfo("Discarding cached location parameters...");
            nearestSpawnPointPositions.Clear();
            OriginalSettings.Clear();
            CurrentLocation = null;
            HasRaidStarted = false;
        }

        public static void SetCurrentLocation(LocationSettingsClass.Location location)
        {
            CurrentLocation = location;
        }

        public static Vector3? GetNearestSpawnPointPosition(Vector3 position, EPlayerSideMask playerSideMask = EPlayerSideMask.All)
        {
            if (CurrentLocation == null)
            {
                return null;
            }

            // Use the cached nearest position if available
            if (nearestSpawnPointPositions.ContainsKey(playerSideMask) && nearestSpawnPointPositions[playerSideMask].ContainsKey(position))
            {
                return nearestSpawnPointPositions[playerSideMask][position];
            }

            Vector3? nearestPosition = null;
            float nearestDistance = float.MaxValue;

            // Find the nearest spawn point to the desired position
            foreach (SpawnPointParams spawnPoint in CurrentLocation.SpawnPointParams)
            {
                // Make sure the spawn point is valid for at least one of the specified player sides
                if (!spawnPoint.Sides.Any(playerSideMask))
                {
                    continue;
                }

                Vector3 spawnPointPosition = spawnPoint.Position.ToUnityVector3();
                float distance = Vector3.Distance(position, spawnPointPosition);
                if (distance < nearestDistance)
                {
                    nearestPosition = spawnPointPosition;
                    nearestDistance = distance;
                }
            }

            // If a spawn point was selected, cache it
            if (nearestPosition.HasValue)
            {
                if (!nearestSpawnPointPositions.ContainsKey(playerSideMask))
                {
                    nearestSpawnPointPositions.Add(playerSideMask, new Dictionary<Vector3, Vector3>());
                }

                nearestSpawnPointPositions[playerSideMask].Add(position, nearestPosition.Value);
            }

            return nearestPosition;
        }

        public static double InterpolateForFirstCol(double[][] array, double value)
        {
            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            if (value <= array[0][0])
            {
                return array[0][1];
            }

            for (int i = 1; i < array.Length; i++)
            {
                if (array[i][0] >= value)
                {
                    if (array[i][0] - array[i - 1][0] == 0)
                    {
                        return array[i][1];
                    }

                    return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
                }
            }

            return array.Last()[1];
        }

        public static double GetLootRemainingFactor(double timeRemainingFactor)
        {
            return InterpolateForFirstCol(ConfigController.Config.LootMultipliers, timeRemainingFactor);
        }

        public static double GetTargetPlayersFullOfLoot(double timeRemainingFactor)
        {
            return InterpolateForFirstCol(ConfigController.Config.FractionOfPlayersFullOfLoot, timeRemainingFactor);
        }

        public static int GetTargetLootSlotsDestroyed(double timeRemainingFactor)
        {
            if (CurrentLocation == null)
            {
                return 0;
            }

            double totalSlots = CurrentLocation.MaxPlayers * ConfigController.Config.DestroyLootDuringRaid.AvgSlotsPerPlayer;
            return (int)Math.Round(GetTargetPlayersFullOfLoot(timeRemainingFactor) * totalSlots);
        }

        public static void AdjustVExChance(LocationSettingsClass.Location location, double timeReductionFactor)
        {
            if (!ConfigController.Config.ScavRaidAdjustments.AdjustVexChance)
            {
                return;
            }

            // Ensure at least one pair exists in the array
            if (ConfigController.Config.VExChanceReductions.Length == 0)
            {
                return;
            }

            if (CarExtractNames.Length == 0)
            {
                LoggingController.Logger.LogInfo("Getting car extract names...");
                CarExtractNames = ConfigController.GetCarExtractNames();
            }

            // Calculate the reduction in VEX chance
            double reductionFactor = InterpolateForFirstCol(ConfigController.Config.VExChanceReductions, timeReductionFactor);

            // Find all VEX extracts and adjust their chances proportionally
            foreach (GClass1135 exit in location.exits)
            {
                if (CarExtractNames.Contains(exit.Name))
                {
                    exit.Chance *= (float)reductionFactor;
                    LoggingController.LogInfo("Vehicle extract " + exit.Name + " chance reduced to " + Math.Round(exit.Chance, 1) + "%");
                }
            }
        }

        public static void AdjustBossSpawnChances(LocationSettingsClass.Location location, double timeReductionFactor)
        {
            if (!ConfigController.Config.AdjustBotSpawnChances.Enabled || !ConfigController.Config.AdjustBotSpawnChances.AdjustBosses)
            {
                return;
            }

            // Calculate the reduction in boss spawn chances
            float reductionFactor = (float)InterpolateForFirstCol(ConfigController.Config.BossSpawnChanceMultipliers, timeReductionFactor);

            foreach (BossLocationSpawn bossLocation in location.BossLocationSpawn)
            {
                if (ConfigController.Config.AdjustBotSpawnChances.ExcludedBosses.Contains(bossLocation.BossName))
                {
                    continue;
                }

                bossLocation.BossChance *= reductionFactor;
                LoggingController.LogInfo("Boss " + bossLocation.BossName + " spawn adjusted to " + Math.Round(bossLocation.BossChance, 1) + "%");
            }
        }

        public static void CacheLocationSettings(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...");

                location.EscapeTimeLimit = OriginalSettings[location.Id].EscapeTimeLimit;

                foreach (GClass1135 exit in location.exits)
                {
                    if (CarExtractNames.Contains(exit.Name))
                    {
                        exit.Chance = OriginalSettings[location.Id].VExChance;
                        LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...Restored VEX chance to " + exit.Chance);
                    }
                }

                if (location.BossLocationSpawn.Length != OriginalSettings[location.Id].BossSpawnChances.Length)
                {
                    throw new InvalidOperationException("Mismatch in length between boss location array and cached array.");
                }

                for (int i = 0; i < location.BossLocationSpawn.Length; i++)
                {
                    location.BossLocationSpawn[i].BossChance = OriginalSettings[location.Id].BossSpawnChances[i];
                    LoggingController.LogInfo("Recalling original raid settings for " + location.Name + "...Restored " + location.BossLocationSpawn[i].BossName + " spawn chance to " + location.BossLocationSpawn[i].BossChance);
                }

                return;
            }

            LoggingController.LogInfo("Storing original raid settings for " + location.Name + "... (Escape time: " + location.EscapeTimeLimit + ")");

            Models.LocationSettings settings = new Models.LocationSettings(location.EscapeTimeLimit);
            
            foreach (GClass1135 exit in location.exits)
            {
                if (CarExtractNames.Contains(exit.Name))
                {
                    settings.VExChance = exit.Chance;
                }
            }

            settings.BossSpawnChances = location.BossLocationSpawn.Select(x => x.BossChance).ToArray();

            OriginalSettings.Add(location.Id, settings);
        }

        public static int GetOriginalEscapeTime(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                return OriginalSettings[location.Id].EscapeTimeLimit;
            }

            throw new InvalidOperationException("The original settings for " + location.Id + " were never stored");
        }
    }
}
