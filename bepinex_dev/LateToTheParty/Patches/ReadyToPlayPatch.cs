using Aki.Reflection.Patching;
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
        }

        private static void RestoreSettings(LocationSettingsClass.Location location)
        {
            if (OriginalSettings.ContainsKey(location.Id))
            {
                location.EscapeTimeLimit = OriginalSettings[location.Id].EscapeTimeLimit;
            }
            else
            {
                OriginalSettings.Add(location.Id, new LocationSettings(location.EscapeTimeLimit));
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

            if (LateToThePartyPlugin.ModConfig.LootMultipliers.Length == 1)
            {
                return LateToThePartyPlugin.ModConfig.LootMultipliers.Last()[1];
            }

            if (timeReductionFactor <= LateToThePartyPlugin.ModConfig.LootMultipliers[0][0])
            {
                return LateToThePartyPlugin.ModConfig.LootMultipliers[0][1];
            }

            double[][] factors = LateToThePartyPlugin.ModConfig.LootMultipliers;
            for (int i = 1; i < factors.Length; i++)
            {
                if (factors[i][0] >= timeReductionFactor)
                {
                    if (factors[i][0] - factors[i - 1][0] == 0)
                    {
                        return factors[i][1];
                    }

                    return factors[i - 1][1] + (timeReductionFactor - factors[i - 1][0]) * (factors[i][1] - factors[i - 1][1]) / (factors[i][0] - factors[i - 1][0]);
                }
            }

            return LateToThePartyPlugin.ModConfig.LootMultipliers.Last()[1];
        }
    }
}
