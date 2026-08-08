using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Helpers
{
    public static class InteractiveObjectHelpers
    {
        public static event Action<WorldInteractiveObject, InteractionResult>? OnExecuteInteraction;
        public static event Action<WorldInteractiveObject, EDoorState>? OnForceDoorState;

        public static string GetText(this WorldInteractiveObject obj) => obj.Id + " (" + (obj.gameObject?.name ?? "???") + ")";
        public static bool CanToggle(this WorldInteractiveObject obj) => obj.Operatable && (obj.gameObject.layer == LayerMask.NameToLayer("Interactive"));

        public static void StartExecuteInteraction(this WorldInteractiveObject interactiveObject, InteractionResult interactionResult)
        {
            interactiveObject.ExecuteInteraction(interactionResult);

            if (OnExecuteInteraction != null)
            {
                OnExecuteInteraction(interactiveObject, interactionResult);
            }
        }

        public static void PrepareInteraction(this WorldInteractiveObject interactiveObject)
        {
            Player? mainPlayer = PlayerHelpers.GetMainPlayer();
            if (mainPlayer == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not identify the main player. This might cause SAIN errors when doors open, but they can be ignored.");
            }

            // Band-aid fix for SAIN errors in DoorSoundPatch if interactiveObject.InteractingPlayer is null
            interactiveObject.SetUser(mainPlayer);

            interactiveObject.LockForInteraction();
        }

        public static void ExecuteInteraction(this WorldInteractiveObject interactiveObject, InteractionResult interactionResult)
        {
            Singleton<LoggingUtil>.Instance.LogInfo("Performing " + interactionResult.InteractionType.ToString() + " on " + interactiveObject.GetType().Name + ": " + interactiveObject.Id + "...");

            interactiveObject.PrepareInteraction();
            interactiveObject.Interact(interactionResult);

            // Only needed if PrepareInteraction sets this to a Player
            interactiveObject.SetUser(null);
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
            Singleton<LoggingUtil>.Instance.LogInfo("Forcing " + interactiveObject.GetType().Name + " " + interactiveObject.Id + " to " + doorState.ToString() + "...");

            interactiveObject.DoorState = doorState;
            interactiveObject.OnEnable();
        }

        public static float GetSwitchTogglingDelayTime(EFT.Interactive.Switch sw1, EFT.Interactive.Switch sw2)
        {
            // Get the delay (in seconds) for one switch to be toggled after another one
            float distance = Vector3.Distance(sw1.transform.position, sw2.transform.position);
            return Singleton<ConfigUtil>.Instance.CurrentConfig.ToggleSwitchesDuringRaid.DelayAfterPressingPrereqSwitchSPerM * distance;
        }
    }
}
