using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty.Components
{
    public class PlayerMonitor : MonoBehaviour
    {
        private Dictionary<Player, Vector3> playerPositionsCurrent = new Dictionary<Player, Vector3>();
        private Dictionary<Player, Vector3> playerPositionsLast = new Dictionary<Player, Vector3>();
        private Stopwatch updateTimer = Stopwatch.StartNew();

        protected void Awake()
        {
            updateForPlayer(Singleton<GameWorld>.Instance.MainPlayer);
        }

        protected void Update()
        {
            if (updateTimer.ElapsedMilliseconds < 100)
            {
                return;
            }

            IEnumerable<Player> humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI);
            foreach (Player player in humanPlayers)
            {
                updateForPlayer(player);
            }
            
            updateTimer.Restart();
        }

        private void updateForPlayer(Player player)
        {
            if (playerPositionsCurrent.ContainsKey(player))
            {
                playerPositionsCurrent[player] = player.Position;
            }
            else
            {
                playerPositionsCurrent.Add(player, player.Position);
                playerPositionsLast.Add(player, player.Position);
            }
        }

        public IEnumerable<Vector3> GetPlayerPositions(bool onlyAlive = true)
        {
            return playerPositionsCurrent
                .Where(p => !onlyAlive || p.Key.HealthController.IsAlive)
                .Select(p => p.Key.Position);
        }

        public IEnumerable<string> GetPlayerIDs(bool onlyAlive = true)
        {
            return playerPositionsCurrent
                .Where(p => !onlyAlive || p.Key.HealthController.IsAlive)
                .Select(p => p.Key.Profile.Id);
        }

        public float GetMostDistanceTravelledByPlayer()
        {
            IEnumerable<float> distancesTravelled = playerPositionsCurrent
                .Select(p => Vector3.Distance(playerPositionsCurrent[p.Key], playerPositionsLast[p.Key]));

            if (!distancesTravelled.Any())
            {
                return 0;
            }

            foreach (Player player in playerPositionsCurrent.Keys)
            {
                playerPositionsLast[player] = playerPositionsCurrent[player];
            }

            return distancesTravelled.Max();
        }

        public Player GetNearestPlayer(Vector3 position, bool onlyAlive = true)
        {
            float minDistance = float.MaxValue;
            Player nearestPlayer = null;

            foreach (Player player in playerPositionsCurrent.Keys)
            {
                if (onlyAlive && !player.HealthController.IsAlive)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, player.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPlayer = player;
                }
            }

            return nearestPlayer;
        }

        public float GetDistanceFromNearestPlayer(Vector3 position, bool onlyAlive = true)
        {
            Player nearestPlayer = GetNearestPlayer(position, onlyAlive);
            if (nearestPlayer == null)
            {
                return float.NaN;
            }

            return Vector3.Distance(position, nearestPlayer.Position);
        }
    }
}
