using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Helpers
{
    public static class ItemHelpers
    {
        private static Dictionary<string, ItemTemplate?> itemTemplates = new Dictionary<string, ItemTemplate?>();

        public static bool TemplateIdExists(string templateId) => GetItemTemplate(templateId) != null;

        public static ItemTemplate? GetItemTemplate(string templateId)
        {
            if (itemTemplates.ContainsKey(templateId))
            {
                return itemTemplates[templateId];
            }

            IEnumerable<ItemTemplate> matchingTemplates = Singleton<ItemFactory>.Instance.ItemTemplates.Values
                .Where(template => template._id.ToString() == templateId);

            int matches = matchingTemplates.Count();
            if (matches > 1)
            {
                Singleton<LoggingUtil>.Instance.LogError("Multiple item templates exist with ID " + templateId);
            }
            if (matches == 0)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not find template ID " + templateId);
            }

            ItemTemplate? template = matches == 1 ? matchingTemplates.First() : null;
            itemTemplates.Add(templateId, template);

            return template;
        }

        public static bool CanRemoveFrom(this Item item, Item parentItem)
        {
            if (item.TemplateId == parentItem.TemplateId)
            {
                return true;
            }

            CompoundItem? compoundItem;
            if ((compoundItem = (parentItem as CompoundItem)) == null)
            {
                return true;
            }

            foreach (Slot slot in compoundItem.Slots)
            {
                /*if (!slot.Required)
                {
                    continue;
                }

                if (slot.Items.Contains(item))
                {
                    return false;
                }*/

                if (slot.RemoveItem(true).Failed)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsLocked(this Item item)
        {
            if (item.PinLockState == EItemPinLockState.Locked)
            {
                //Singleton<LoggingUtil>.Instance.LogWarning(item.LocalizedName() + " is locked", true);
                return true;
            }

            SlotItemAddress? parentSlot = item.Parent as SlotItemAddress;
            if ((parentSlot != null) && parentSlot.Slot.Locked)
            {
                //Singleton<LoggingUtil>.Instance.LogWarning(item.LocalizedName() + " is locked inside " + parentSlot.ContainerName.Localized(), true);
                return true;
            }

            StackSlotItemAddress? parentStackSlot = item.Parent as StackSlotItemAddress;
            if ((parentStackSlot != null) && (parentStackSlot.StackSlot.Items.Count() > 0))
            {
                // Weird EFT edge case with ammo boxes
                if (parentStackSlot.StackSlot.Items.Last() == item)
                {
                    //Singleton<LoggingUtil>.Instance.LogWarning(item.LocalizedName() + " is locked inside stack " + parentStackSlot.ContainerName.Localized(), true);
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<Item> FindAllItemsInContainer(this Item container, bool includeSelf = false)
        {
            List<Item> allContainedItems = container.GetAllItems().ToList();
            
            if (!includeSelf)
            {
                allContainedItems.Remove(container);
            }

            foreach (Item item in allContainedItems.ToArray())
            {
                allContainedItems.AddRange(item.FindAllItemsInContainer(false));
            }

            return allContainedItems.Distinct();
        }

        public static IEnumerable<Item> FindAllItemsInContainers(this IEnumerable<Item> containers, bool includeSelf = false)
        {
            List<Item> allItems = new List<Item>();
            foreach (Item container in containers)
            {
                allItems.AddRange(container.FindAllItemsInContainer(includeSelf));
            }
            return allItems.Distinct();
        }

        public static IEnumerable<Item> FindAllRelatedItems(this IEnumerable<Item> items)
        {
            List<Item> allItems = new List<Item>();
            foreach (Item item in items)
            {
                Item parentItem = item.GetAllParentItemsAndSelf().Last();
                allItems.AddRange(parentItem.GetAllItems().Reverse());
            }
            return allItems.Distinct();
        }

        public static int GetItemSlots(this Item item)
        {
            int itemSlots = 0;
            if (Singleton<ConfigUtil>.Instance.LootRanking?.ContainsKey(item.TemplateId) == true)
            {
                itemSlots = (int)Singleton<ConfigUtil>.Instance.LootRanking[item.TemplateId].Size;
            }
            else
            {
                var itemSize = item.CalculateCellSize();
                itemSlots = itemSize.X * itemSize.Y;
            }

            return itemSlots;
        }
    }
}
