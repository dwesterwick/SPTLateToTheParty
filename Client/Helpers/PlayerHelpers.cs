using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LateToTheParty.Helpers
{
    public static class PlayerHelpers
    {
        public static Player? GetMainPlayer()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            // Fika Headless client does not have a MainPlayer
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return GetFirstHumanPlayer();
            }

            return Singleton<GameWorld>.Instance.MainPlayer;
        }

        public static Player? GetFirstHumanPlayer()
        {
            IEnumerable<Player> humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI);
            if (humanPlayers.Any())
            {
                return humanPlayers.First();
            }

            return null;
        }

        public static bool AnyHumanPlayersWithinRange(this Vector3 sourcePosition, double range)
        {
            IEnumerable<Player> humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI);
            if (humanPlayers.Any(player => Vector3.Distance(player.Position, sourcePosition) <= range))
            {
                return true;
            }

            return false;
        }
    }
}
