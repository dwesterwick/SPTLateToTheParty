using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace LateToTheParty.Models
{
    public static class ItemHelpers
    {
        public static IEnumerable<Item> FindAllItemsInContainer(this Item container, bool includeSelf = false)
        {
            IEnumerable<Item> containedItems = container.GetAllItems();

            if (!includeSelf)
            {
                containedItems = containedItems.Where(i => i.Id != container.Id);
            }

            foreach (Item item in containedItems)
            {
                containedItems.Concat(item.FindAllItemsInContainer(false));
            }

            return containedItems.Distinct();
        }

        public static IEnumerable<Item> FindAllItemsInContainers(this IEnumerable<Item> containers, bool includeSelf = false)
        {
            IEnumerable<Item> allItems = Enumerable.Empty<Item>();
            foreach (Item container in containers)
            {
                allItems = allItems.Concat(container.FindAllItemsInContainer(includeSelf));
            }
            return allItems.Distinct();
        }

        public static IEnumerable<Item> FindAllRelatedItems(this IEnumerable<Item> items)
        {
            IEnumerable<Item> allItems = Enumerable.Empty<Item>();
            foreach (Item item in items)
            {
                Item parentItem = item.GetAllParentItemsAndSelf().Last();
                allItems = allItems.Concat(parentItem.GetAllItems().Reverse());
            }
            return allItems.Distinct();
        }

        public static IEnumerable<string> GetSecureContainerIDs()
        {
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            if (itemFactory == null)
            {
                return Enumerable.Empty<string>();
            }

            // Find all possible secure containers
            List<string> secureContainerIDs = new List<string>();
            foreach (Item item in itemFactory.CreateAllItemsEver())
            {
                if (!(item.Template is SecureContainerTemplateClass))
                {
                    continue;
                }

                if (!(item.Template as SecureContainerTemplateClass).isSecured)
                {
                    continue;
                }

                secureContainerIDs.Add(item.TemplateId);
            }

            return secureContainerIDs;
        }

        public static double GetDistanceToNearestSpawnPoint(Vector3 position)
        {
            return GetDistanceToNearestSpawnPoint(position, Patches.ReadyToPlayPatch.LastLocationSelected, EPlayerSideMask.Pmc);
        }

        public static double GetDistanceToNearestSpawnPoint(Vector3 position, LocationSettingsClass.Location location, EPlayerSideMask playerSideMask)
        {
            double distance = double.MaxValue;

            foreach (EFT.Game.Spawning.SpawnPointParams spawnPoint in location.SpawnPointParams)
            {
                if (!spawnPoint.Sides.HasFlag(playerSideMask))
                {
                    continue;
                }

                double _dist = Vector3.Distance(position, spawnPoint.Position.ToUnityVector3());
                if (_dist < distance)
                {
                    distance = _dist;
                }
            }

            if (distance == double.MaxValue)
            {
                return 0;
            }

            return distance;
        }
    }
}
