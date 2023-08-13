using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.BotLogic
{
    internal class PMCObjective : MonoBehaviour
    {
        public bool IsObjectiveActive { get; private set; } = false;
        public bool IsObjectiveReached { get; set; } = false;
        public bool CanChangeObjective { get; set; } = true;
        public bool CanRushPlayerSpawn { get; private set; } = false;
        public bool CanReachObjective { get; set; } = true;
        public float MinTimeAtObjective { get; set; } = 10f;
        public Vector3? Position { get; set; } = null;

        private BotOwner botOwner = null;
        private LocationSettingsClass.Location location = null;
        private SpawnPointParams? targetSpawnPoint = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();
        private Stopwatch timeSinceChangingObjectiveTimer = Stopwatch.StartNew();
        private List<SpawnPointParams> blacklistedSpawnPoints = new List<SpawnPointParams>();

        public Vector3 PlayerPosition
        {
            get { return getPlayerPosition(); }
        }

        public double TimeSpentAtObjective
        {
            get { return timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public double TimeSinceChangingObjective
        {
            get { return timeSinceChangingObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            IsObjectiveActive = botOwner.Side != EPlayerSide.Savage;
            CanRushPlayerSpawn = BotGenerator.IsBotFromInitialPMCSpawns(botOwner);
        }

        public void ChangeObjective()
        {
            if (!CanChangeObjective)
            {
                return;
            }

            if (targetSpawnPoint.HasValue && !blacklistedSpawnPoints.Contains(targetSpawnPoint.Value))
            {
                blacklistedSpawnPoints.Add(targetSpawnPoint.Value);
            }

            if (TryToGoToRandomQuestObjective())
            {
                return;
            }

            SpawnPointParams? newSpawnPoint = getNewObjective();
            if (!newSpawnPoint.HasValue)
            {
                LoggingController.LogError("Could not find any valid objectives for bot " + botOwner.Profile.Nickname);
                IsObjectiveActive = false;
                return;
            }

            updateObjective(newSpawnPoint.Value);
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

            if (!Position.HasValue && !targetSpawnPoint.HasValue)
            {
                ChangeObjective();
            }

            if (!Position.HasValue && targetSpawnPoint.HasValue)
            {
                Position = targetSpawnPoint.Value.Position;
            }
        }

        public float GetRaidET()
        {
            // Get the current number of seconds remaining and elapsed in the raid
            float escapeTimeSec = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (LocationSettingsController.LastOriginalEscapeTime * 60f) - escapeTimeSec;

            return raidTimeElapsed;
        }

        private SpawnPointParams? getNewObjective()
        {
            float distanceToPlayer = GetDistanceToPlayer();

            if (CanRushPlayerSpawn && (GetRaidET() < 999) && (distanceToPlayer < 75))
            {
                SpawnPointParams playerSpawnPoint = getPlayerSpawnPoint();
                if (!blacklistedSpawnPoints.Contains(playerSpawnPoint))
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is heading to your spawn point!");
                    return playerSpawnPoint;
                }
            }

            SpawnPointParams? randomSpawnPoint = getRandomSpawnPoint(ESpawnCategoryMask.Bot, ESpawnCategoryMask.Player, 25);
            if (randomSpawnPoint.HasValue)
            {
                return randomSpawnPoint.Value;
            }

            return null;
        }

        private bool TryToGoToRandomQuestObjective()
        {
            Models.Quest quest = BotQuestController.getRandomQuestInCurrentLocation();
            if (quest == null)
            {
                LoggingController.LogWarning("Could not get a quest for bot " + botOwner.Profile.Nickname);
                return false;
            }

            string zoneID = BotQuestController.getRandomZoneIDForQuest(quest);
            if (zoneID == null)
            {
                LoggingController.LogWarning("Could not get a target zone for quest " + quest.Name + " for bot " + botOwner.Profile.Nickname);
                return false;
            }

            Vector3? targetPosition = quest.GetPositionForZoneID(zoneID);
            if (!targetPosition.HasValue)
            {
                LoggingController.LogWarning("Could not get a position for target zone " + zoneID + " for quest " + quest.Name + " for bot " + botOwner.Profile.Nickname);
                return false;
            }

            updateObjective(targetPosition.Value, zoneID + " for quest " + quest.Name);

            return true;
        }

        private void updateObjective(SpawnPointParams newTarget)
        {
            targetSpawnPoint = newTarget;
            updateObjective(targetSpawnPoint.Value.Position, "Spawn point " + targetSpawnPoint.Value.Id);
        }

        private void updateObjective(Vector3 newTargetPosition, string objectiveDescription)
        {
            Position = newTargetPosition;
            IsObjectiveReached = false;

            timeSinceChangingObjectiveTimer.Restart();
            LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has a new objective: " + objectiveDescription);
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
