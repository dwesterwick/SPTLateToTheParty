using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.BotLogic
{
    internal class PMCObjective : MonoBehaviour
    {
        public bool IsObjectiveActive { get; private set; } = false;
        public bool IsObjectiveReached { get; set; } = false;
        public Vector3? Position { get; set; } = null;

        private BotOwner botOwner = null;
        private LocationSettingsClass.Location location = null;
        private SpawnPointParams? targetSpawnPoint = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();

        public Vector3 PlayerPosition
        {
            get { return getPlayerPosition(); }
        }

        public double TimeSpentAtObjective
        {
            get { return timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            IsObjectiveActive = BotGenerator.IsBotFromInitialPMCSpawns(botOwner);
        }

        public void ChangeObjective()
        {
            updateObjective(getNewObjective());
        }

        public float GetDistanceToPlayer()
        {
            return Vector3.Distance(PlayerPosition, botOwner.Position);
        }

        private void Update()
        {
            if (!IsObjectiveActive)
            {
                return;
            }

            if (IsObjectiveReached)
            {
                timeSpentAtObjectiveTimer.Start();
            }
            else
            {
                timeSpentAtObjectiveTimer.Reset();
            }

            if (location == null)
            {
                location = LocationSettingsController.LastLocationSelected;
            }

            if (!targetSpawnPoint.HasValue)
            {
                ChangeObjective();
            }

            if (!Position.HasValue)
            {
                Position = targetSpawnPoint.Value.Position;
            }
        }

        private SpawnPointParams getNewObjective()
        {
            float distanceToPlayer = GetDistanceToPlayer();

            if (distanceToPlayer < 50)
            {
                return getPlayerSpawnPoint();
            }

            return getRandomSpawnPoint(ESpawnCategoryMask.Bot, ESpawnCategoryMask.Player);
        }

        private void updateObjective(SpawnPointParams newTarget)
        {
            targetSpawnPoint = newTarget;
            Position = targetSpawnPoint.Value.Position;
            IsObjectiveReached = false;

            LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has a new objective: " + targetSpawnPoint.Value.Id);
        }

        private Vector3 getPlayerPosition()
        {
            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        private SpawnPointParams getRandomSpawnPoint(ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All, ESpawnCategoryMask blacklistedSpawnTypes = ESpawnCategoryMask.None, float minDistance = 0)
        {
            return location.SpawnPointParams
                .Where(s => s.Categories.Any(spawnTypes))
                .Where(s => !s.Categories.Any(blacklistedSpawnTypes))
                .Where(s => Vector3.Distance(s.Position, botOwner.Position) >= minDistance)
                .Random();
        }

        private SpawnPointParams getPlayerSpawnPoint()
        {
            return BotGenerator.GetNearestSpawnPoint(PlayerPosition, location.SpawnPointParams);
        }
    }
}
