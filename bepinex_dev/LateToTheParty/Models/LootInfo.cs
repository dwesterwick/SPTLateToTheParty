﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public enum ELootType
    {
        Invalid = 0,
        Loose = 1,
        Static = 2,
    }

    public class LootInfo
    {
        public ELootType LootType { get; } = ELootType.Invalid;
        public bool IsDestroyed { get; set; } = false;
        public TraderControllerClass TraderController { get; set; } = null;
        public Transform Transform { get; set; } = null;
        public double DistanceToNearestSpawnPoint { get; set; } = 0;

        public LootInfo(ELootType lootType)
        {
            LootType = lootType;
        }

        public LootInfo(ELootType lootType, TraderControllerClass traderController, Transform transform) : this(lootType)
        {
            TraderController = traderController;
            Transform = transform;
        }

        public LootInfo(ELootType lootType, TraderControllerClass traderController, Transform transform, double distanceToNearestSpawnPoint) : this(lootType, traderController, transform)
        {
            DistanceToNearestSpawnPoint = distanceToNearestSpawnPoint;
        }
    }
}