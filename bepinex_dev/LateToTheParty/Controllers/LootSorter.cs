using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT.InventoryLogic;
using LateToTheParty.Models;
using System.Collections;

namespace LateToTheParty.Controllers
{
    internal class LootSorter
    {
        private static Dictionary<Item, LootInfo> LootInfo = new Dictionary<Item, LootInfo>();
        private static IEnumerable<KeyValuePair<Item, LootInfo>> SortedLoot = null;

        public LootSorter(Dictionary<Item, LootInfo> _LootInfo)
        {
            LootInfo = _LootInfo;
        }

        public IEnumerable<Item> FindLootToDestroy(double targetLootRemainingFraction, double lastLootDestroyedET)
        {
            if (SortedLoot == null)
            {
                throw new InvalidOperationException("Loot must be sorted before it can be destroyed.");
            }

            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = (double)LootInfo.Values.Where(v => v.IsDestroyed == false).Count() / LootInfo.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //LoggingController.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + currentLootRemainingFraction);

            // Calculate the number of loot items to destroy
            int lootItemsToDestroy = (int)Math.Floor(Math.Max(0, lootFractionToDestroy) * LootInfo.Count);
            if (lootItemsToDestroy == 0)
            {
                if (lastLootDestroyedET >= ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot * 1000.0)
                {
                    LoggingController.LogInfo("Max time of " + ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot + "s elapsed since destroying loot. Forcing at least 1 item to be removed...");
                    lootItemsToDestroy = 1;
                }
                else
                {
                    return Enumerable.Empty<Item>();
                }
            }

            // Generate a list of loot to be destroyed. This needs to be iterated because each item in the loot dictionaries has an unknown number of child items in it. 
            int actualLootBeingDestroyed = 0;
            IEnumerable<KeyValuePair<Item, LootInfo>> lootToDestroy = Enumerable.Empty<KeyValuePair<Item, LootInfo>>();
            foreach (KeyValuePair<Item, LootInfo> lootInfo in SortedLoot)
            {
                if (actualLootBeingDestroyed >= lootItemsToDestroy)
                {
                    break;
                }

                lootToDestroy = lootToDestroy.Append(lootInfo);
                actualLootBeingDestroyed += lootInfo.Key.ToEnumerable().FindAllRelatedItems().Count();
            }

            //LoggingController.LogInfo("Target loot to destroy: " + lootItemsToDestroy + ", Loot Being Destroyed: " + actualLootBeingDestroyed);

            return lootToDestroy.Select(l => l.Key);
        }

        public IEnumerator SortLoot(Vector3 yourPosition, double raidET)
        {
            System.Random randomGen = new System.Random();

            // Find all loot items eligible for destruction and sort them            
            IEnumerable<KeyValuePair<Item, LootInfo>> eligibleItems = LootInfo.Where(l => LootManager.CanDestroyItem(l.Key, yourPosition, raidET));

            // Get the loot ranking data from the server, but this only needs to be done once
            if (ConfigController.LootRanking == null)
            {
                ConfigController.GetLootRankingData();
            }
            if (ConfigController.LootRanking == null)
            {
                LoggingController.LogError("Cannot read loot ranking data from the server.");
            }

            yield return SortLootDictionary(eligibleItems.ToDictionary(i => i.Key, i => i.Value));
        }

        private IEnumerator SortLootDictionary(Dictionary<Item, LootInfo> input)
        {
            System.Random randomGen = new System.Random();
            Dictionary<Item, LootInfo> inputDict = input.ToDictionary(i => i.Key, i => i.Value);
            Item[] inputKeys = inputDict.Keys.ToArray();
            Dictionary<Item, double> inputValues = new Dictionary<Item, double>();

            // Determine how much randomness to apply to loot sorting
            double lootValueRange = ConfigController.LootRanking.Items.Max(i => i.Value.Value) - ConfigController.LootRanking.Items.Min(i => i.Value.Value);
            double lootValueRandomFactor = lootValueRange * ConfigController.Config.DestroyLootDuringRaid.LootRanking.Randomness / 100.0;

            foreach (Item inputKey in inputKeys)
            {
                if ((!ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled) || (ConfigController.LootRanking == null))
                {
                    inputValues.Add(inputKey, randomGen.NextDouble());
                }
                else
                {
                    inputValues.Add(inputKey, ConfigController.LootRanking.Items[inputKey.TemplateId].Value + randomGen.Range(-1, 1) * lootValueRandomFactor);
                }
            }

            // Spread the work across multiple frames based on a maximum calculation time per frame
            EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
            object[] sortingOutputs = enumeratorWithTimeLimit.Sort(inputValues).ToEnumerable().ToArray();
            Dictionary<Item, double> outputValues = (Dictionary<Item, double>)sortingOutputs.First(o => o is Dictionary<Item, double>);

            SortedLoot = outputValues.ToDictionary(i => i.Key, i => inputDict[i.Key]);

            yield return null;
        }
    }
}
