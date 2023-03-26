﻿using Aki.Reflection.Patching;
using EFT.UI.Matchmaker;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;

namespace LateToTheParty.Patches
{
    public class ReadyToPlayPatch : ModulePatch
    {
        private static Dictionary<string, LocationSettings> OriginalSettings = new Dictionary<string, LocationSettings>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("method_45", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(bool __result, RaidSettings ___raidSettings_0)
        {
            if (!__result)
            {
                return;
            }

            LocationSettingsClass.Location location = ___raidSettings_0.SelectedLocation;
            RestoreSettings(location);

            double timeReductionFactor = GenerateTimeReductionFactor(___raidSettings_0.IsScav);
            if (timeReductionFactor == 1)
            {
                Logger.LogInfo("Using original settings");
                return;
            }

            location.EscapeTimeLimit = (int)(location.EscapeTimeLimit * timeReductionFactor);
            Logger.LogInfo("Changed escape time to " + location.EscapeTimeLimit);

            double lootMultiplierFactor = CalculateLootMultiplier(timeReductionFactor);
            Logger.LogInfo("Adjusting loot multipliers by " + lootMultiplierFactor);
            Controllers.ConfigController.SetLootMultipliers(lootMultiplierFactor);

            AdjustTrainTimes(location);

            double vexChanceFactor = Interpolate(LateToThePartyPlugin.ModConfig.VExChanceReductions, timeReductionFactor);
            AdjustVExChance(location, vexChanceFactor);
        }

        private static void RestoreSettings(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                location.EscapeTimeLimit = OriginalSettings[location.Id].EscapeTimeLimit;
                foreach (GClass1195 exit in location.exits)
                {
                    if (exit.PassageRequirement == EFT.Interactive.ERequirementState.Train)
                    {
                        exit.Count = OriginalSettings[location.Id].TrainWaitTime;
                        exit.MinTime = OriginalSettings[location.Id].TrainMinTime;
                        exit.MaxTime = OriginalSettings[location.Id].TrainMaxTime;
                    }

                    if (LateToThePartyPlugin.CarExtractNames.Contains(exit.Name))
                    {
                        exit.Chance = OriginalSettings[location.Id].VExChance;
                    }
                }
            }
            else
            {
                LocationSettings settings = new LocationSettings(location.EscapeTimeLimit);
                foreach (GClass1195 exit in location.exits)
                {
                    if (exit.PassageRequirement == EFT.Interactive.ERequirementState.Train)
                    {
                        settings.TrainWaitTime = exit.Count;
                        settings.TrainMinTime = exit.MinTime;
                        settings.TrainMaxTime = exit.MaxTime;
                    }

                    if (LateToThePartyPlugin.CarExtractNames.Contains(exit.Name))
                    {
                        settings.VExChance = exit.Chance;
                    }
                }
                OriginalSettings.Add(location.Id, settings);
            }
        }

        private static double GenerateTimeReductionFactor(bool isScav)
        {
            Random random = new Random();

            Configuration.EscapeTimeConfig config = isScav ? LateToThePartyPlugin.ModConfig.Scav : LateToThePartyPlugin.ModConfig.PMC;

            if (random.NextDouble() > config.Chance)
            {
                return 1;
            }

            return (config.TimeFactorMax - config.TimeFactorMin) * random.NextDouble() + config.TimeFactorMin;
        }

        private static double CalculateLootMultiplier(double timeReductionFactor)
        {
            if (LateToThePartyPlugin.ModConfig.LootMultipliers.Length == 0)
            {
                return 1;
            }

            return Interpolate(LateToThePartyPlugin.ModConfig.LootMultipliers, timeReductionFactor);
        }

        private static double Interpolate(double[][] array, double value)
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

        private static void AdjustTrainTimes(LocationSettingsClass.Location location)
        {
            int timeReduction = (OriginalSettings[location.Id].EscapeTimeLimit - location.EscapeTimeLimit) * 60;

            foreach(GClass1195 exit in location.exits)
            {
                if (exit.PassageRequirement != EFT.Interactive.ERequirementState.Train)
                {
                    continue;
                }

                exit.MaxTime -= timeReduction;
                exit.MinTime -= timeReduction;

                if (exit.MaxTime < 60)
                {
                    exit.MinTime += (60 - exit.MaxTime);
                    exit.MaxTime = 60;
                }

                if (exit.MinTime >= exit.MaxTime)
                {
                    exit.MinTime = exit.MaxTime - 1;
                }

                Logger.LogInfo("Train extract " + exit.Name + ": MaxTime=" + exit.MaxTime + ", MinTime=" + exit.MinTime);
            }
        }

        private static void AdjustVExChance(LocationSettingsClass.Location location, double reductionFactor)
        {
            foreach (GClass1195 exit in location.exits)
            {
                if (LateToThePartyPlugin.CarExtractNames.Contains(exit.Name))
                {
                    exit.Chance *= (float)reductionFactor;
                    Logger.LogInfo("Vehicle extract " + exit.Name + " chance reduced to " + exit.Chance);
                }
            }
        }
    }
}
