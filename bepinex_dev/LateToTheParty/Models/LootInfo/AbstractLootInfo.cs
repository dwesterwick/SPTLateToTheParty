using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace LateToTheParty.Models.LootInfo
{
    public abstract class AbstractLootInfo
    {
        public abstract TraderControllerClass TraderController { get; }
        public abstract Transform Transform { get; }
        public abstract string LootTypeName { get; }

        public double DistanceToNearestSpawnPoint { get; protected set; } = 0;
        public double? RaidETWhenFound { get; private set; } = null;

        public PathAccessibilityData PathData { get; } = new PathAccessibilityData();
        public bool CannotBeDestroyed { get; set; } = false;
        public bool IsDestroyed { get; set; } = false;
        public bool IsInPlayerInventory { get; set; } = false;
        public double? RaidETWhenDestroyed { get; set; } = null;
        public bool EligibleForDestruction { get; set; } = false;
        public Item ParentItem { get; set; } = null;
        public WorldInteractiveObject ParentContainer { get; set; } = null;
        public WorldInteractiveObject NearbyInteractiveObject { get; set; } = null;

        public AbstractLootInfo(double distanceToNearestSpawnPoint, double raidET)
        {
            DistanceToNearestSpawnPoint = distanceToNearestSpawnPoint;
            RaidETWhenFound = raidET;
        }
    }
}
