using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.BotLogic
{
    internal class PMCDefaultAction : CustomLogic
    {
        private GClass177 defaultAction;

        public PMCDefaultAction(BotOwner botOwner) : base(botOwner)
        {
            defaultAction = new GClass177(botOwner);
        }

        public override void Update()
        {
            defaultAction.Update();
        }
    }
}
