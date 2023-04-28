using System;
using System.Collections;
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
        public static int InteractiveLayer { get; set; }

        private static Dictionary<Door, bool> canCloseDoors = new Dictionary<Door, bool>();
        private static MethodInfo canStartInteractionMethodInfo = typeof(WorldInteractiveObject).GetMethod("CanStartInteraction", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static int doorsToToggle = 1;
        private static int validDoors = -1;

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
                updateTimer.Restart();

                validDoors = -1;

                return;
            }

            // Ensure enough time has passed since the last door event
            if ((updateTimer.ElapsedMilliseconds < ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents * 1000) && (canCloseDoors.Count > 0))
            {
                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1426.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (LocationSettingsController.LastOriginalEscapeTime * 60f) - escapeTimeSec;

            // Don't run the script before the raid begins
            if (raidTimeElapsed < 3)
            {
                return;
            }

            // Do not change doors too early or late into the raid
            if ((canCloseDoors.Count > 0) && ((raidTimeElapsed < ConfigController.Config.OpenDoorsDuringRaid.MinRaidET) || (escapeTimeSec < ConfigController.Config.OpenDoorsDuringRaid.MinRaidTimeRemaining)))
            {
                return;
            }

            // Only find doors once per raid
            doorsToToggle = (int)Math.Max(1, Math.Round(canCloseDoors.Count * ConfigController.Config.OpenDoorsDuringRaid.PercentageOfDoorsPerEvent / 100.0));
            if (validDoors == -1)
            {
                FindAllValidDoors();
                validDoors = canCloseDoors.Count;

                doorsToToggle *= (int)Math.Ceiling(Math.Max(raidTimeElapsed - ConfigController.Config.OpenDoorsDuringRaid.MinRaidET, 0) / ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents);
            }

            // Ensure there are doors to toggle
            if (doorsToToggle == 0)
            {
                return;
            }

            // Try to change the state of doors
            StartCoroutine(ToggleDoors(doorsToToggle));
            updateTimer.Restart();
        }

        private IEnumerator ToggleDoors(int doorsToToggle)
        {
            // Spread the work across multiple frames based on a maximum calculation time per frame
            EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
            yield return enumeratorWithTimeLimit.Run(Enumerable.Repeat(1, doorsToToggle), ToggleRandomDoor, doorsToToggle);
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
                //Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                //GClass2644 availableActions = GClass1767.GetAvailableActions(, door);

                if (!door.Operatable)
                {
                    LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is inoperable.");
                    continue;
                }

                if (door.gameObject.layer != InteractiveLayer)
                {
                    LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is inoperable (wrong layer).");
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

        private void ToggleRandomDoor(int maxCalcTime_ms, int totalDoorsToToggle)
        {
            // Randomly sort eligible doors
            System.Random randomObj = new System.Random();
            IEnumerable<Door> randomlyOrderedKeys = canCloseDoors.OrderBy(e => randomObj.NextDouble()).Select(d => d.Key);

            // Try to find a door to toggle, but do not wait too long
            Stopwatch calcTimer = Stopwatch.StartNew();
            while (calcTimer.ElapsedMilliseconds < maxCalcTime_ms)
            {
                // Randomly select a new door state
                EDoorState newState = EDoorState.Open;
                if (randomObj.Next(0, 100) < ConfigController.Config.OpenDoorsDuringRaid.ChanceOfClosingDoors)
                {
                    newState = EDoorState.Shut;
                }

                // Try to make the desired change to each door in the randomly-sorted enumerator
                foreach (Door door in randomlyOrderedKeys)
                {
                    //LoggingController.LogInfo("Attempting to change door " + door.Id + " to " + newState + "...");
                    if (ToggleDoor(door, newState))
                    {
                        return;
                    }
                }
            }
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
