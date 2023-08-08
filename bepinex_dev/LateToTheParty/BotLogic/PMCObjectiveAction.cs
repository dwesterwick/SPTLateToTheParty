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
        private GClass274 baseSteeringLogic = new GClass274();
        private bool canRun = false;

        public PMCObjectiveAction(BotOwner _botOwner) : base(_botOwner)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.GetComponent<PMCObjective>();
        }

        public override void Start()
        {
            botOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            botOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            // Look where you're going
            botOwner.SetPose(1f);
            botOwner.Steering.LookToMovingDirection();
            botOwner.SetTargetMoveSpeed(1f);

            if (botOwner.GetPlayer.Physical.Stamina.NormalValue > 0.5f)
            {
                botOwner.GetPlayer.EnableSprint(true);
                canRun = true;
            }
            if (botOwner.GetPlayer.Physical.Stamina.NormalValue < 0.1f)
            {
                botOwner.GetPlayer.EnableSprint(false);
                canRun = false;
            }

            botOwner.DoorOpener.Update();
            baseSteeringLogic.Update(botOwner);

            if (!objective.IsObjectiveActive || !objective.Position.HasValue)
            {
                return;
            }

            if (!objective.IsObjectiveReached && Vector3.Distance(objective.Position.Value, botOwner.Position) < 10f)
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " reached its objective.");
                objective.IsObjectiveReached = true;
            }
            else
            {
                bool isMovingToObjective = TryGoToObjective(objective.Position.Value);

                if (!isMovingToObjective)
                {
                    LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot go to its objective. Setting another one...");
                    objective.ChangeObjective();
                }
            }
        }

        public bool TryGoToObjective(Vector3 position)
        {
            if (!canRun)
            {
                NavMeshPathStatus? pathStatus = botOwner.Mover?.GoToPoint(position, true, 0.5f, false, false);

                if (!pathStatus.HasValue)
                {
                    return false;
                }

                return pathStatus.Value == NavMeshPathStatus.PathComplete;
            }
            else
            {
                return botOwner.BotRun.Run(position, false);
            }
        }
    }
}
