using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using LateToTheParty.Controllers;

namespace LateToTheParty.Helpers
{
    public static class InteractiveObjectHelpers
    {
        public static void ExecuteInteraction(this WorldInteractiveObject interactiveObject, InteractionResult interactionResult)
        {
            LoggingController.LogInfo("Performing " + interactionResult.InteractionType.ToString() + " on " + interactiveObject.GetType().Name + ": " + interactiveObject.Id + "...");

            // NOTE: This does NOT work with Fika. Fika requires player.vmethod_1 to be called, but this forces animations. 
            interactiveObject.LockForInteraction();
            interactiveObject.Interact(interactionResult);
        }
    }
}
