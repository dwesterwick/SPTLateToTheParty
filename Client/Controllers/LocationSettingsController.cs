using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using LateToTheParty.Helpers;
using LateToTheParty.Utils;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LocationSettingsController
    {
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null!;

        private static Dictionary<string, Models.LocationSettings> OriginalSettings = new Dictionary<string, Models.LocationSettings>();
        private static Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>> nearestSpawnPointPositions = new Dictionary<EPlayerSideMask, Dictionary<Vector3, Vector3>>();
        
        public static void ClearOriginalSettings()
        {
            Singleton<LoggingUtil>.Instance.LogInfo("Discarding cached location parameters...");
            nearestSpawnPointPositions.Clear();
            OriginalSettings.Clear();
            CurrentLocation = null!;
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

        public static double GetLootRemainingFactor(double timeRemainingFactor)
        {
            return SharedConfigHelpers.InterpolateForFirstCol(Singleton<ConfigUtil>.Instance.CurrentConfig.LootMultipliers, timeRemainingFactor);
        }

        public static double GetTargetPlayersFullOfLoot(double timeRemainingFactor)
        {
            double fraction = SharedConfigHelpers.InterpolateForFirstCol(Singleton<ConfigUtil>.Instance.CurrentConfig.FractionOfPlayersFullOfLoot, timeRemainingFactor);
            
            // Reduce the amount of loot "slots" that can be destroyed if player Scavs are not allowed to spwan into the map
            if (CurrentLocation.DisabledForScav)
            {
                fraction *= Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.PlayersWithLootFactorForMapsWithoutPscavs;
            }

            return fraction;
        }

        public static int GetTargetLootSlotsDestroyed(double timeRemainingFactor)
        {
            if (CurrentLocation == null)
            {
                return 0;
            }

            double totalSlots = CurrentLocation.MaxPlayers * Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.AvgSlotsPerPlayer;
            return (int)Math.Round(GetTargetPlayersFullOfLoot(timeRemainingFactor) * totalSlots);
        }

        public static void AdjustVExChance(LocationSettingsClass.Location location, float chance)
        {
            foreach (LocationExitClass exit in location.exits)
            {
                if (CarExtractHelpers.IsCarExtract(exit.Name))
                {
                    exit.Chance = chance;
                    Singleton<LoggingUtil>.Instance.LogInfo("Vehicle extract " + exit.Name + " chance adjusted to " + Math.Round(exit.Chance, 1) + "%");
                }
            }
        }

        public static void AdjustBossSpawnChances(LocationSettingsClass.Location location, double timeReductionFactor)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustBotSpawnChances.Enabled || !Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustBotSpawnChances.AdjustBosses)
            {
                return;
            }

            // Calculate the reduction in boss spawn chances
            float reductionFactor = (float)SharedConfigHelpers.InterpolateForFirstCol(Singleton<ConfigUtil>.Instance.CurrentConfig.BossSpawnChanceMultipliers, timeReductionFactor);

            foreach (BossLocationSpawn bossLocation in location.BossLocationSpawn)
            {
                if (Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustBotSpawnChances.ExcludedBosses.Contains(bossLocation.BossName))
                {
                    continue;
                }

                bossLocation.BossChance *= reductionFactor;
                Singleton<LoggingUtil>.Instance.LogInfo("Boss " + bossLocation.BossName + " spawn adjusted to " + Math.Round(bossLocation.BossChance, 1) + "%");
            }
        }

        public static void CacheLocationSettings(LocationSettingsClass.Location location)
        {
            try
            {
                if (OriginalSettings.ContainsKey(location.Id))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Recalling original raid settings for " + location.Id + "...");

                    location.EscapeTimeLimit = OriginalSettings[location.Id].EscapeTimeLimit;

                    foreach (LocationExitClass exit in location.exits)
                    {
                        if (CarExtractHelpers.IsCarExtract(exit.Name))
                        {
                            exit.Chance = OriginalSettings[location.Id].VExChance;
                            Singleton<LoggingUtil>.Instance.LogInfo("Recalling original raid settings for " + location.Id + "...Restored VEX chance to " + exit.Chance);
                        }
                    }

                    if (location.BossLocationSpawn.Length != OriginalSettings[location.Id].BossSpawnChances.Length)
                    {
                        throw new InvalidOperationException("Mismatch in length between boss location array and cached array.");
                    }

                    for (int i = 0; i < location.BossLocationSpawn.Length; i++)
                    {
                        location.BossLocationSpawn[i].BossChance = OriginalSettings[location.Id].BossSpawnChances[i];
                        Singleton<LoggingUtil>.Instance.LogInfo("Recalling original raid settings for " + location.Id + "...Restored " + location.BossLocationSpawn[i].BossName + " spawn chance to " + location.BossLocationSpawn[i].BossChance);
                    }

                    return;
                }

                Singleton<LoggingUtil>.Instance.LogInfo("Storing original raid settings for " + location.Id + "... (Escape time: " + location.EscapeTimeLimit + ")");

                Models.LocationSettings settings = new Models.LocationSettings(location.EscapeTimeLimit);

                foreach (LocationExitClass exit in location.exits)
                {
                    if (CarExtractHelpers.IsCarExtract(exit.Name))
                    {
                        settings.VExChance = exit.Chance;
                    }
                }

                settings.BossSpawnChances = location.BossLocationSpawn.Select(x => x.BossChance).ToArray();

                OriginalSettings.Add(location.Id, settings);
            }
            catch (Exception ex)
            {
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole("Could not store original raid settings for " + location.Id + ": " + ex.Message);
                Singleton<LoggingUtil>.Instance.LogError(ex.StackTrace);
            }
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
