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
        private List<SpawnPointParams> blacklistedSpawnPoints = new List<SpawnPointParams>();

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
            if (targetSpawnPoint.HasValue)
            {
                blacklistedSpawnPoints.Add(targetSpawnPoint.Value);
            }

            SpawnPointParams? newSpawnPoint = getNewObjective();
            if (newSpawnPoint.HasValue)
            {
                updateObjective(newSpawnPoint.Value);
            }
            else
            {
                LoggingController.LogWarning("Could not find any valid objectives for bot " + botOwner.Profile.Nickname);
                IsObjectiveActive = false;
            }
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

        private SpawnPointParams? getNewObjective()
        {
            float distanceToPlayer = GetDistanceToPlayer();

            if (distanceToPlayer < 50)
            {
                SpawnPointParams playerSpawnPoint = getPlayerSpawnPoint();
                if (!blacklistedSpawnPoints.Contains(playerSpawnPoint))
                {
                    return playerSpawnPoint;
                }
            }

            SpawnPointParams? randomSpawnPoint = getRandomSpawnPoint(ESpawnCategoryMask.Bot, ESpawnCategoryMask.Player);
            if (randomSpawnPoint.HasValue)
            {
                return randomSpawnPoint.Value;
            }

            return null;
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

        private SpawnPointParams? getRandomSpawnPoint(ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All, ESpawnCategoryMask blacklistedSpawnTypes = ESpawnCategoryMask.None, float minDistance = 0)
        {
            IEnumerable<SpawnPointParams> possibleSpawnPoints = location.SpawnPointParams
                .Where(s => !blacklistedSpawnPoints.Any(b => b.Id == s.Id))
                .Where(s => s.Categories.Any(spawnTypes))
                .Where(s => !s.Categories.Any(blacklistedSpawnTypes))
                .Where(s => Vector3.Distance(s.Position, botOwner.Position) >= minDistance);

            if (possibleSpawnPoints.IsNullOrEmpty())
            {
                return null;
            }

            return possibleSpawnPoints.Random();
        }

        private SpawnPointParams getPlayerSpawnPoint()
        {
            return BotGenerator.GetNearestSpawnPoint(PlayerPosition, location.SpawnPointParams);
        }
    }
}
