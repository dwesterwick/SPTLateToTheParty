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
    public static class ItemHelpers
    {
        private static Dictionary<string, Item> allItems = new Dictionary<string, Item>();

        public static bool CanRemoveFrom(this Item item, Item parentItem)
        {
            if (item.TemplateId == parentItem.TemplateId)
            {
                return true;
            }

            CompoundItem compoundItem;
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
                //LoggingController.LogWarning(item.LocalizedName() + " is locked", true);
                return true;
            }

            GClass3113 parentSlot = item.Parent as GClass3113;
            if ((parentSlot != null) && parentSlot.Slot.Locked)
            {
                //LoggingController.LogWarning(item.LocalizedName() + " is locked inside " + parentSlot.ContainerName.Localized(), true);
                return true;
            }

            GClass3114 parentStackSlot = item.Parent as GClass3114;
            if (parentStackSlot != null)
            {
                // Weird EFT edge case with ammo boxes
                if (parentStackSlot.StackSlot.Items.IndexOf(item) != parentStackSlot.StackSlot.Items.Count() - 1)
                {
                    //LoggingController.LogWarning(item.LocalizedName() + " is locked inside stack " + parentStackSlot.ContainerName.Localized(), true);
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
            if (Controllers.ConfigController.LootRanking?.Items?.ContainsKey(item.TemplateId) == true)
            {
                itemSlots = (int)Controllers.ConfigController.LootRanking.Items[item.TemplateId].Size;
            }
            else
            {
                var itemSize = item.CalculateCellSize();
                itemSlots = itemSize.X * itemSize.Y;
            }

            return itemSlots;
        }

        public static Dictionary<string, Item> GetAllItems()
        {
            if (allItems.Count > 0)
            {
                return allItems;
            }

            ItemFactoryClass itemFactory = Singleton<ItemFactoryClass>.Instance;
            if (itemFactory == null)
            {
                return allItems;
            }

            foreach(Item item in itemFactory.CreateAllItemsEver())
            {
                allItems.Add(item.TemplateId, item);
            }

            Controllers.LoggingController.LogInfo("Created dictionary of " + allItems.Count + " items");
            return allItems;
        }
    }
}
