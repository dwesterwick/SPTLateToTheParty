using DrakiaXYZ.BigBrain.Brains;
using EFT;
using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.BotLogic
{
    internal class PMCObjectiveLayer : CustomLayer
    {
        private PMCObjective objective;
        private BotOwner botOwner;

        public PMCObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.AddComponent<PMCObjective>();
            objective.Init(botOwner);
        }

        public override string GetName()
        {
            return "PMCObjectiveLayer";
        }

        public override Action GetNextAction()
        {
            if (!objective.IsObjectiveActive)
            {
                return new Action(typeof(PMCDefaultAction), "NoObjectiveSet");
            }

            if (objective.IsObjectiveReached)
            {
                if (objective.TimeSpentAtObjective > 30)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has spent " + objective.TimeSpentAtObjective + "s at its objective. Setting a new one...");
                    objective.ChangeObjective();
                }
                else
                {
                    return new Action(typeof(PMCDefaultAction), "ObjectiveReached");
                }
            }

            return new Action(typeof(PMCObjectiveAction), "GoToObjective");
        }

        public override bool IsActive()
        {
            return objective.IsObjectiveActive && !objective.IsObjectiveReached;
        }

        public override bool IsCurrentActionEnding()
        {
            return objective.IsObjectiveReached || !objective.IsObjectiveActive;
        }
    }
}
