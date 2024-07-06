using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class PlayerMonitorController : MonoBehaviour
    {
        private static Dictionary<Player, Vector3> playerPositionsCurrent = new Dictionary<Player, Vector3>();
        private static Dictionary<Player, Vector3> playerPositionsLast = new Dictionary<Player, Vector3>();
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                Clear();
                return;
            }

            if (updateTimer.ElapsedMilliseconds < 100)
            {
                return;
            }

            IEnumerable<Player> humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI);
            foreach (Player player in humanPlayers)
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
            
            updateTimer.Restart();
        }

        public static void Clear()
        {
            playerPositionsCurrent.Clear();
            playerPositionsLast.Clear();
        }

        public static IEnumerable<Vector3> GetPlayerPositions(bool onlyAlive = true)
        {
            return playerPositionsCurrent
                .Where(p => !onlyAlive || p.Key.HealthController.IsAlive)
                .Select(p => p.Key.Position);
        }

        public static IEnumerable<string> GetPlayerIDs(bool onlyAlive = true)
        {
            return playerPositionsCurrent
                .Where(p => !onlyAlive || p.Key.HealthController.IsAlive)
                .Select(p => p.Key.Profile.Id);
        }

        public static float GetMostDistanceTravelledByPlayer()
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

        public static Player GetNearestPlayer(Vector3 position, bool onlyAlive = true)
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

        public static float GetDistanceFromNearestPlayer(Vector3 position, bool onlyAlive = true)
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
