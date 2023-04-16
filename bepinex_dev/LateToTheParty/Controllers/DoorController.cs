using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.Models;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class DoorController : MonoBehaviour
    {
        private List<Door> validDoors = new List<Door>();
        private static MethodInfo canStartInteractionMethodInfo = typeof(WorldInteractiveObject).GetMethod("CanStartInteraction", BindingFlags.NonPublic | BindingFlags.Instance);

        private void Update()
        {
            if (!ConfigController.Config.OpenDoorsDuringRaid.Enabled)
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                validDoors.Clear();
                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1426.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (LocationSettingsController.LastOriginalEscapeTime * 60f) - escapeTimeSec;

            // Don't run the script in the Hideout
            if (escapeTimeSec > 3600 * 24 * 90)
            {
                return;
            }

            // Do not change doors too early or late into the raid
            if ((raidTimeElapsed < ConfigController.Config.OpenDoorsDuringRaid.MinRaidET) || (escapeTimeSec < ConfigController.Config.OpenDoorsDuringRaid.MinRaidTimeRemaining))
            {
                return;
            }

            if (validDoors.Count == 0)
            {
                FindAllValidDoors();
            }

            ToggleDoors();
        }

        private void FindAllValidDoors()
        {
            validDoors.Clear();

            LoggingController.LogInfo("Searching for valid doors...");
            Door[] allDoors = UnityEngine.Object.FindObjectsOfType<Door>();
            LoggingController.LogInfo("Searching for valid doors...found " + allDoors.Length + " possible doors.");

            foreach (Door door in allDoors)
            {
                // The following checks are from DrakiaXYZ's DoorRandomizer mod
                if (!door.Operatable)
                {
                    LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is inoperable.");
                    continue;
                }
                
                if (door.DoorState != EDoorState.Open && door.DoorState != EDoorState.Shut && door.DoorState != EDoorState.Locked)
                {
                    LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " has an invalid state: " + door.DoorState);
                    continue;
                }

                Transform pushTransform = door.transform.Find("Push");
                Transform pullTransform = door.transform.Find("Pull");
                bool canPush = (pushTransform != null) && pushTransform.gameObject.activeInHierarchy;
                bool canPull = (pullTransform != null) && pullTransform.gameObject.activeInHierarchy;
                if (!canPush || !canPull)
                {
                    LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " cannot be opened (Push: " + canPush + ", Pull: " + canPull + ")");
                    continue;
                }

                validDoors.Add(door);
            }

            LoggingController.LogInfo("Searching for valid doors...found " + validDoors.Count + " doors.");
        }

        private void ToggleDoors()
        {
            foreach (Door door in validDoors)
            {
                // Ignore doors that are too close to you
                Vector3 yourPosition = Camera.main.transform.position;
                float doorDist = Vector3.Distance(yourPosition, door.transform.position);
                if (doorDist < ConfigController.Config.OpenDoorsDuringRaid.ExclusionRadius)
                {
                    continue;
                }

                if (door.DoorState == EDoorState.Locked)
                {
                    LoggingController.LogInfo("Unlocking door: " + door.Id);
                    door.DoorState = EDoorState.Shut;
                    door.OnEnable();

                    // This doesn't work
                    //door.Interact(new GClass2599(EInteractionType.Unlock));
                }
                
                if (door.DoorState != EDoorState.Open)
                {
                    // Ignore doors that are currently being opened/closed                    
                    if (!(bool)canStartInteractionMethodInfo.Invoke(door, new object[] { EDoorState.Open, true }))
                    {
                        continue;
                    }

                    LoggingController.LogInfo("Opening door: " + door.Id);
                    //door.DoorState = EDoorState.Open;
                    //door.OnEnable();

                    // This plays the opening noise and animation
                    door.Interact(new GClass2599(EInteractionType.Open));
                }
            }
        }
    }
}
