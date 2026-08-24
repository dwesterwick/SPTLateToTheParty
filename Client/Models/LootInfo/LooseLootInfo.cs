using EFT.Interactive;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Models.LootInfo
{
    public class LooseLootInfo : AbstractLootInfo
    {
        private static string lootTypeName = "LooseLoot";
        private LootItem lootItem;

        public override ItemController ItemOwner => lootItem.ItemOwner;
        public override Transform Transform => lootItem.transform;
        public override string LootTypeName => lootTypeName;

        public LooseLootInfo(LootItem _lootItem, double distanceToNearestSpawnPoint, double raidET) : base(distanceToNearestSpawnPoint, raidET)
        {
            lootItem = _lootItem;
        }
    }
}
