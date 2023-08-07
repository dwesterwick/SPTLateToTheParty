using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.BotLogic
{
    internal class PMCObjective : MonoBehaviour
    {
        public bool IsObjectiveReached { get; set; } = false;
        public Vector3? Position { get; set; } = null;

        private LocationSettingsClass.Location location = null;
        private SpawnPointParams? targetSpawnPoint = null;

        public Vector3 PlayerPosition
        {
            get { return getPlayerPosition(); }
        }

        private void Update()
        {
            if (location == null)
            {
                location = LocationSettingsController.LastLocationSelected;
            }

            if (!targetSpawnPoint.HasValue)
            {
                targetSpawnPoint = getPlayerSpawnPoint();
            }

            if (!Position.HasValue)
            {
                Position = targetSpawnPoint.Value.Position;
            }
        }

        private Vector3 getPlayerPosition()
        {
            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        private SpawnPointParams getRandomSpawnPoint()
        {
            return location.SpawnPointParams.Random();
        }

        private SpawnPointParams getPlayerSpawnPoint()
        {
            return BotGenerator.GetNearestSpawnPoint(PlayerPosition, location.SpawnPointParams);
        }
    }
}
