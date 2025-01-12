﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using LateToTheParty.Helpers;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LocationSettingsController
    {
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;

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

        public static double GetLootRemainingFactor(double timeRemainingFactor)
        {
            return ConfigController.InterpolateForFirstCol(ConfigController.Config.LootMultipliers, timeRemainingFactor);
        }

        public static double GetTargetPlayersFullOfLoot(double timeRemainingFactor)
        {
            double fraction = ConfigController.InterpolateForFirstCol(ConfigController.Config.FractionOfPlayersFullOfLoot, timeRemainingFactor);
            
            // Reduce the amount of loot "slots" that can be destroyed if player Scavs are not allowed to spwan into the map
            if (CurrentLocation.DisabledForScav)
            {
                fraction *= ConfigController.Config.DestroyLootDuringRaid.PlayersWithLootFactorForMapsWithoutPScavs;
            }

            return fraction;
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

        public static void AdjustVExChance(LocationSettingsClass.Location location, float chance)
        {
            foreach (LocationExitClass exit in location.exits)
            {
                if (CarExtractHelpers.IsCarExtract(exit.Name))
                {
                    exit.Chance = chance;
                    LoggingController.LogInfo("Vehicle extract " + exit.Name + " chance adjusted to " + Math.Round(exit.Chance, 1) + "%");
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
            float reductionFactor = (float)ConfigController.InterpolateForFirstCol(ConfigController.Config.BossSpawnChanceMultipliers, timeReductionFactor);

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

                foreach (LocationExitClass exit in location.exits)
                {
                    if (CarExtractHelpers.IsCarExtract(exit.Name))
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
