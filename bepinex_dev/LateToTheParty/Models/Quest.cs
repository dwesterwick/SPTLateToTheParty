using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Models
{
    public class Quest
    {
        public RawQuestClass Template { get; private set; }
        public int MinLevel { get; set; }
        
        private Dictionary<string, Vector3> zoneIDsAndPositions = new Dictionary<string, Vector3>();
        private Dictionary<LootItem, Vector3> itemsAndPositions = new Dictionary<LootItem, Vector3>();

        public string Name => Template.Name;
        public string TemplateId => Template.TemplateId;
        public string[] ZoneIDs => zoneIDsAndPositions.Keys.ToArray();
        public LootItem[] Items => itemsAndPositions.Keys.ToArray();

        public Quest(RawQuestClass template)
        {
            Template = template;
        }

        public void ClearPositionData()
        {
            zoneIDsAndPositions.Clear();
            itemsAndPositions.Clear();
        }

        public Vector3? GetPositionForZoneID(string zoneID)
        {
            if (!zoneIDsAndPositions.ContainsKey(zoneID))
            {
                return null;
            }

            return zoneIDsAndPositions[zoneID];
        }

        public Vector3? GetPositionForItem(LootItem item)
        {
            if (!itemsAndPositions.ContainsKey(item))
            {
                return null;
            }

            return itemsAndPositions[item];
        }

        public LootItem FindLootItem(string templateID)
        {
            IEnumerable<LootItem> matchingItems = Items.Where(i => i.TemplateId == templateID);
            if (matchingItems.Count() != 1)
            {
                return null;
            }

            return matchingItems.First();
        }

        public void AddZonesWithoutPosition(IEnumerable<string> zoneIDs)
        {
            foreach(string zoneID in zoneIDs)
            {
                AddZoneWithoutPosition(zoneID);
            }
        }

        public void AddZoneWithoutPosition(string zoneID)
        {
            if (zoneIDsAndPositions.ContainsKey(zoneID))
            {
                return;
            }

            zoneIDsAndPositions.Add(zoneID, Vector3.positiveInfinity);
        }

        public void AddZoneAndPosition(string zoneID, Vector3 position)
        {
            if (zoneIDsAndPositions.ContainsKey(zoneID))
            {
                zoneIDsAndPositions[zoneID] = position;
                return;
            }

            zoneIDsAndPositions.Add(zoneID, position);
        }

        public void AddItemAndPosition(LootItem item, Vector3 position)
        {
            if (itemsAndPositions.ContainsKey(item))
            {
                itemsAndPositions[item] = position;
                return;
            }

            itemsAndPositions.Add(item, position);
        }
    }
}
