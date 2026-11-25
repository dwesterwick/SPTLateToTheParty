using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Components;
using LateToTheParty.Utils;

namespace LateToTheParty.Helpers.Loot
{
    internal static class LootFilteringHelpers
    {
        public static IEnumerable<Item> RemoveItemsDroppedByPlayer(this IEnumerable<Item> items)
        {
            return items.Where(i => !Singleton<LootDestroyerComponent>.Instance.LootManager.WasDroppedByPlayer(i));
        }

        public static IEnumerable<Item> RemoveExcludedItems(this IEnumerable<Item> items)
        {
            foreach (Item item in items)
            {
                if (Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => p == item.TemplateId))
                {
                    continue;
                }

                if (item.Template.Parent != null && Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => item.Template.IsChildOf(p)))
                {
                    continue;
                }

                if (EFT.UI.DragAndDrop.ItemViewFactory.IsSecureContainer(item))
                {
                    continue;
                }

                yield return item;
            }
        }

        public static IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> RemoveItemsWithoutValidTemplates(this IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> lootInfo)
        {
            // If loot ranking is disabled or invalid, this cannot be performed
            if
            (
                !Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.LootRanking.Enabled
                || (Singleton<ConfigUtil>.Instance.LootRanking == null)
                || (Singleton<ConfigUtil>.Instance.LootRanking.Count == 0)
            )
            {
                return lootInfo;
            }

            foreach (KeyValuePair<Item, Models.LootInfo.AbstractLootInfo> item in lootInfo)
            {
                if (Singleton<ConfigUtil>.Instance.LootRanking.ContainsKey(item.Key.TemplateId))
                {
                    continue;
                }

                Singleton<LoggingUtil>.Instance.LogWarning("Preventing " + item.Key.LocalizedName() + " from being destroyed because it does not have a valid template (" + item.Key.TemplateId + ")");
            }

            return lootInfo.Where(l => Singleton<ConfigUtil>.Instance.LootRanking.ContainsKey(l.Key.TemplateId));
        }
    }
}
