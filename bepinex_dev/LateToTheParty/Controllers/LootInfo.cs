using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class LootInfo
    {
        public bool IsDestroyed { get; set; } = false;
        public TraderControllerClass TraderController { get; set; } = null;
        public Transform Transform { get; set; } = null;
        public double DistanceToNearestSpawnPoint = 0;

        public LootInfo()
        {

        }

        public LootInfo(TraderControllerClass traderController, Transform transform) : this()
        {
            TraderController = traderController;
            Transform = transform;
        }

        public LootInfo(TraderControllerClass traderController, Transform transform, double distanceToNearestSpawnPoint) : this(traderController, transform)
        {
            DistanceToNearestSpawnPoint = distanceToNearestSpawnPoint;
        }
    }
}
