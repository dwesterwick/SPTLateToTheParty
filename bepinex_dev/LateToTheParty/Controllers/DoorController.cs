using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<Door, bool> canCloseDoors = new Dictionary<Door, bool>();
        private static MethodInfo canStartInteractionMethodInfo = typeof(WorldInteractiveObject).GetMethod("CanStartInteraction", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void Update()
        {
            if (!ConfigController.Config.OpenDoorsDuringRaid.Enabled)
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                canCloseDoors.Clear();
                return;
            }

            // Ensure enough time has passed since the last door event
            if (updateTimer.ElapsedMilliseconds < ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents * 1000)
            {
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

            // Only find doors once per raid
            if (canCloseDoors.Count == 0)
            {
                FindAllValidDoors();
            }

            // Try to change the state of a door
            if (ToggleRandomDoor())
            {
                updateTimer.Restart();
            }
        }

        private void FindAllValidDoors()
        {
            canCloseDoors.Clear();

            LoggingController.LogInfo("Searching for valid doors...");
            Door[] allDoors = UnityEngine.Object.FindObjectsOfType<Door>();
            LoggingController.LogInfo("Searching for valid doors...found " + allDoors.Length + " possible doors.");

            // Get all items to search for key ID's
            Dictionary<string, Item> allItems = ItemHelpers.GetAllItems();

            foreach (Door door in allDoors)
            {
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

                if (door.DoorState == EDoorState.Locked)
                {
                    if (allItems.ContainsKey(door.KeyId) && !ConfigController.Config.OpenDoorsDuringRaid.CanOpenLockedDoors)
                    {
                        LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is locked and not allowed to be opened.");
                        continue;
                    }

                    if (!allItems.ContainsKey(door.KeyId) && !door.CanBeBreached)
                    {
                        LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is locked and has no valid key.");
                        continue;
                    }

                    if (door.CanBeBreached && !ConfigController.Config.OpenDoorsDuringRaid.CanBreachDoors)
                    {
                        LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is not allowed to be breached.");
                        continue;
                    }
                }

                // If the door is eligible for toggling during the raid, add it to the dictionary
                canCloseDoors.Add(door, CanCloseDoor(door));
            }

            LoggingController.LogInfo("Searching for valid doors...found " + canCloseDoors.Count + " doors.");
        }

        private bool CanCloseDoor(Door door)
        {
            // This is taken from DrakiaXYZ's Door Randomizer mod
            Transform pushTransform = door.transform.Find("Push");
            Transform pullTransform = door.transform.Find("Pull");
            bool canPush = (pushTransform != null) && pushTransform.gameObject.activeInHierarchy;
            bool canPull = (pullTransform != null) && pullTransform.gameObject.activeInHierarchy;
            if (!canPush || !canPull)
            {
                //LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " cannot be opened (Push: " + canPush + ", Pull: " + canPull + ")");
                return false;
            }

            return true;
        }

        private bool ToggleRandomDoor()
        {
            // Randomly select a new door state
            System.Random randomObj = new System.Random();
            EDoorState newState = EDoorState.Open;
            if (randomObj.Next(0, 100) < ConfigController.Config.OpenDoorsDuringRaid.ChanceOfClosingDoors)
            {
                newState = EDoorState.Shut;
            }

            // Toggle the first door that can be changed to the new state
            IEnumerable<Door> randomlyOrderedKeys = canCloseDoors.OrderBy(e => randomObj.NextDouble()).Select(d => d.Key);
            foreach (Door door in randomlyOrderedKeys)
            {
                //LoggingController.LogInfo("Attempting to change door " + door.Id + " to " + newState + "...");
                if (ToggleDoor(door, newState))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ToggleDoor(Door door, EDoorState newState)
        {
            // Check if the door is already in the desired state
            if (newState == EDoorState.Shut && (door.DoorState == EDoorState.Shut || door.DoorState == EDoorState.Locked))
            {
                return false;
            }
            if (newState == EDoorState.Open && door.DoorState == EDoorState.Open)
            {
                return false;
            }

            // Ensure the player can interact with the door before closing it
            if ((newState == EDoorState.Shut) && !canCloseDoors[door])
            {
                return false;
            }

            // Ignore doors that are too close to you
            Vector3 yourPosition = Camera.main.transform.position;
            float doorDist = Vector3.Distance(yourPosition, door.transform.position);
            if (doorDist < ConfigController.Config.OpenDoorsDuringRaid.ExclusionRadius)
            {
                return false;
            }

            // Unlock or "breach" the door if necessary
            if ((door.DoorState == EDoorState.Locked) && (newState == EDoorState.Open))
            {
                if (door.KeyId.Length > 0)
                {
                    LoggingController.LogInfo("Unlocking door: " + door.Id + " (Key ID: " + door.KeyId + ")");
                }
                else
                {
                    LoggingController.LogInfo("Preparing to breach door: " + door.Id);
                }

                door.DoorState = EDoorState.Shut;
                door.OnEnable();

                // This doesn't work
                //door.Interact(new GClass2599(EInteractionType.Unlock));

                // This never seems to change, so don't bother running it
                /*// Check again if the door can be opened/closed
                bool canCloseDoor = CanCloseDoor(door);
                if (canCloseDoors[door] != canCloseDoor)
                {
                    LoggingController.LogInfo("Allowed to close status changed for door " + door.Id + ": " + canCloseDoor);
                    canCloseDoors[door] = canCloseDoor;
                }*/
            }

            // Ignore doors that are currently being opened/closed                    
            if (!(bool)canStartInteractionMethodInfo.Invoke(door, new object[] { newState, true }))
            {
                return false;
            }

            if ((door.DoorState != EDoorState.Open) && (door.DoorState != EDoorState.Locked) && (newState == EDoorState.Open))
            {
                LoggingController.LogInfo("Opening door: " + door.Id);
                //door.DoorState = EDoorState.Open;
                //door.OnEnable();

                // This plays the opening noise and animation
                door.Interact(new GClass2599(EInteractionType.Open));
                return true;
            }

            if ((door.DoorState == EDoorState.Open) && (newState == EDoorState.Shut))
            {
                LoggingController.LogInfo("Closing door: " + door.Id);
                //door.DoorState = EDoorState.Open;
                //door.OnEnable();

                // This plays the opening noise and animation
                door.Interact(new GClass2599(EInteractionType.Close));
                return true;
            }

            return false;
        }
    }
}
