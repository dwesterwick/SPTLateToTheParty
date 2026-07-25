using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using UnityEngine;

namespace LateToTheParty.Models.LootInfo
{
    public class ContainerLootInfo : AbstractLootInfo
    {
        private static string lootTypeName = "ContainerLoot";
        private LootableContainer lootableContainer;

        public override TraderControllerClass ItemOwner => lootableContainer.ItemOwner;
        public override Transform Transform => lootableContainer.transform;
        public override string LootTypeName => lootTypeName;

        public ContainerLootInfo(LootableContainer _lootableContainer, double distanceToNearestSpawnPoint, double raidET) : base(distanceToNearestSpawnPoint, raidET)
        {
            lootableContainer = _lootableContainer;
        }
    }
}
