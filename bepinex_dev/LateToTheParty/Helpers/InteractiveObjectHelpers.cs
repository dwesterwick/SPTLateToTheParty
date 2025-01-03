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
        public static event Action<WorldInteractiveObject, InteractionResult> OnExecuteInteraction;
        public static event Action<WorldInteractiveObject, EDoorState> OnForceDoorState;

        public static void StartExecuteInteraction(this WorldInteractiveObject interactiveObject, InteractionResult interactionResult)
        {
            interactiveObject.ExecuteInteraction(interactionResult);

            if (OnExecuteInteraction != null)
            {
                OnExecuteInteraction(interactiveObject, interactionResult);
            }
        }

        public static void ExecuteInteraction(this WorldInteractiveObject interactiveObject, InteractionResult interactionResult)
        {
            LoggingController.LogInfo("Performing " + interactionResult.InteractionType.ToString() + " on " + interactiveObject.GetType().Name + ": " + interactiveObject.Id + "...");

            interactiveObject.LockForInteraction();
            interactiveObject.Interact(interactionResult);
        }

        public static void StartForceDoorState(this WorldInteractiveObject interactiveObject, EDoorState doorState)
        {
            interactiveObject.ForceDoorState(doorState);

            if (OnForceDoorState != null)
            {
                OnForceDoorState(interactiveObject, doorState);
            }
        }

        public static void ForceDoorState(this WorldInteractiveObject interactiveObject, EDoorState doorState)
        {
            LoggingController.LogInfo("Forcing " + interactiveObject.GetType().Name + " " + interactiveObject.Id + " to " + doorState.ToString() + "...");

            interactiveObject.DoorState = doorState;
            interactiveObject.OnEnable();
        }
    }
}
