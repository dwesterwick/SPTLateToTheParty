using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;

namespace LateToTheParty.Helpers
{
    internal static class LootFilteringHelpers
    {
        private static string[] secureContainerIDs = new string[0];

        public static IEnumerable<Item> RemoveItemsDroppedByPlayer(this IEnumerable<Item> items) => items.Where(i => !i.WasDroppedByPlayer());

        public static IEnumerable<Item> RemoveExcludedItems(this IEnumerable<Item> items)
        {
            // This should only be run once to generate the array of secure container ID's
            if (secureContainerIDs.Length == 0)
            {
                secureContainerIDs = getSecureContainerIDs().ToArray();
            }

            IEnumerable<Item> filteredItems = items
                .Where(i => i.Template.Parent == null || !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => i.Template.IsChildOf(p)))
                .Where(i => !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => p == i.TemplateId))
                .Where(i => !secureContainerIDs.Contains(i.TemplateId.ToString()));

            return filteredItems;
        }

        private static IEnumerable<string> getSecureContainerIDs()
        {
            ItemFactoryClass itemFactory = Singleton<ItemFactoryClass>.Instance;
            if (itemFactory == null)
            {
                return Enumerable.Empty<string>();
            }

            // Find all possible secure containers
            List<string> secureContainerIDs = new List<string>();
            foreach (Item item in itemFactory.CreateAllItemsEver())
            {
                if (!EFT.UI.DragAndDrop.ItemViewFactory.IsSecureContainer(item))
                {
                    continue;
                }

                secureContainerIDs.Add(item.TemplateId);
            }

            return secureContainerIDs;
        }

        public static IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> RemoveItemsWithoutValidTemplates(this IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> lootInfo)
        {
            // If loot ranking is disabled or invalid, this cannot be performed
            if
            (
                !ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled
                || (ConfigController.LootRanking == null)
                || (ConfigController.LootRanking.Items.Count == 0)
            )
            {
                return lootInfo;
            }

            foreach (KeyValuePair<Item, Models.LootInfo.AbstractLootInfo> item in lootInfo)
            {
                if (ConfigController.LootRanking.Items.ContainsKey(item.Key.TemplateId))
                {
                    continue;
                }

                LoggingController.LogWarning("Preventing " + item.Key.LocalizedName() + " from being destroyed because it does not have a valid template (" + item.Key.TemplateId + ")");
            }

            return lootInfo.Where(l => ConfigController.LootRanking.Items.ContainsKey(l.Key.TemplateId));
        }
    }
}
