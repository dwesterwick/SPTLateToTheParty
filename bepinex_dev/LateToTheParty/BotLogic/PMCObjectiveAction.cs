using DrakiaXYZ.BigBrain.Brains;
using EFT;
using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.BotLogic
{
    internal class PMCObjectiveAction : CustomLogic
    {
        private PMCObjective objective;
        private BotOwner botOwner;
        private bool canRun = false;

        public PMCObjectiveAction(BotOwner _botOwner) : base(_botOwner)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.GetComponent<PMCObjective>();
        }

        public override void Start()
        {
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            if (botOwner.GetPlayer.Physical.Stamina.NormalValue > 0.5f)
            {
                canRun = true;
            }
            if (botOwner.GetPlayer.Physical.Stamina.NormalValue < 0.1f)
            {
                canRun = false;
            }

            if (!objective.Position.HasValue)
            {
                return;
            }

            if (!objective.IsObjectiveReached && Vector3.Distance(objective.Position.Value, BotOwner.Position) < 10f)
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " reached its objective.");
                objective.IsObjectiveReached = true;
            }
            else
            {
                bool isMovingToObjective = TryGoToObjective(objective.Position.Value);

                if (!isMovingToObjective)
                {
                    LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot go to its objective.");
                }
            }
        }

        public bool TryGoToObjective(Vector3 position)
        {
            if (!canRun)
            {
                NavMeshPathStatus? pathStatus = BotOwner.Mover?.GoToPoint(position, true, 0.5f, false, false);

                if (!pathStatus.HasValue)
                {
                    return false;
                }

                return pathStatus.Value == NavMeshPathStatus.PathComplete;
            }
            else
            {
                return BotOwner.BotRun.Run(position, false);
            }
        }
    }
}
