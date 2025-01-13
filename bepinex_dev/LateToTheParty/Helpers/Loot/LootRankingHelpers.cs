using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;

namespace LateToTheParty.Helpers.Loot
{
    public static class LootRankingHelpers
    {
        private static double lootValueRandomFactor = 0;

        public static void ResetLootValueRandomFactor() => lootValueRandomFactor = 0;

        public static IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> Sort(this IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> loot)
        {
            System.Random random = new System.Random();

            // Get the loot ranking data from the server, but this only needs to be done once
            if (ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled && (ConfigController.LootRanking == null))
            {
                ConfigController.GetLootRankingData();
            }

            // If loot ranking is disabled or invalid, simply sort the loot randomly
            if
            (
                !ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled
                || (ConfigController.LootRanking == null)
                || (ConfigController.LootRanking.Items.Count == 0)
            )
            {
                return loot.OrderBy(i => random.NextDouble());
            }

            // Determine how much randomness to apply to loot sorting
            if (lootValueRandomFactor == 0)
            {
                double lootValueRange = getLootValueRange(loot);
                lootValueRandomFactor = lootValueRange * ConfigController.Config.DestroyLootDuringRaid.LootRanking.Randomness / 100.0;
            }

            //LoggingController.LogInfo("Randomness factor: " + lootValueRandomFactor);

            // Return loot sorted by value but with randomness applied
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> sortedLoot = loot.OrderByDescending(i => ConfigController.LootRanking.Items[i.Key.TemplateId].Value + (random.Range(-1, 1) * lootValueRandomFactor));
            return sortedLoot.Skip(ConfigController.Config.DestroyLootDuringRaid.LootRanking.TopValueRetainCount);
        }

        private static double getLootValueRange(IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> loot)
        {
            // Calculate the values of all of the loot on the map
            List<double> lootValues = new List<double>();
            foreach (KeyValuePair<Item, Models.LootInfo.AbstractLootInfo> lootItem in loot)
            {
                if (!ConfigController.LootRanking.Items.ContainsKey(lootItem.Key.TemplateId))
                {
                    LoggingController.LogWarning("Cannot find " + lootItem.Key.LocalizedName() + " in loot-ranking data.");
                    continue;
                }

                double? value = ConfigController.LootRanking.Items[lootItem.Key.TemplateId].Value;
                if (!value.HasValue)
                {
                    LoggingController.LogWarning("The value of " + lootItem.Key.LocalizedName() + " is null in the loot-ranking data.");
                    continue;
                }

                lootValues.Add(value.Value);
            }

            // Calculate the standard deviation of the loot values on the map
            double lootValueAvg = lootValues.Average();
            double lootValueStdev = 0;
            foreach (double val in lootValues)
            {
                lootValueStdev += Math.Pow(val - lootValueAvg, 2);
            }
            lootValueStdev = Math.Sqrt(lootValueStdev / lootValues.Count);

            // Return the range of 2*sigma of the loot values on the map
            return lootValueStdev * 4;
        }

    }
}
