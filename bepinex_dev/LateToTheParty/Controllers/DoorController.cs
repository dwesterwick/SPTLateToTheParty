using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.CoroutineExtensions;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class DoorController : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsTogglingDoors { get; private set; } = false;
        public static bool IsFindingDoors { get; private set; } = false;
        public static bool HasToggledInitialDoors { get; private set; } = false;
        public static int InteractiveLayer { get; set; } = LayerMask.NameToLayer("Interactive");

        private static List<Door> toggleableDoors = new List<Door>();
        private static List<Door> eligibleDoors = new List<Door>();
        private static Dictionary<Door, bool> allowedToToggleDoor = new Dictionary<Door, bool>();
        private GamePlayerOwner gamePlayerOwner = null;
        private static MethodInfo canStartInteractionMethodInfo = typeof(WorldInteractiveObject).GetMethod("CanStartInteraction", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Stopwatch updateTimer = Stopwatch.StartNew();
        private static Stopwatch doorOpeningsTimer = new Stopwatch();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
        private static int doorsToToggle = 1;

        public static int ToggleableDoorCount
        {
            get { return IsFindingDoors ? 0 : toggleableDoors.Count; }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Update()
        {
            if (IsClearing)
            {
                return;
            }

            if (!ConfigController.Config.OpenDoorsDuringRaid.Enabled)
            {
                // Need to do this or it will prevent loot from being despawned
                HasToggledInitialDoors = true;

                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(Clear());
                doorOpeningsTimer.Reset();

                return;
            }

            if (!LocationSettingsController.HasRaidStarted)
            {
                return;
            }

            // Wait until the previous task completes
            if (IsTogglingDoors || IsFindingDoors)
            {
                return;
            }

            // Ensure enough time has passed since the last door event
            if (HasToggledInitialDoors && (updateTimer.ElapsedMilliseconds < ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents * 1000))
            {
                return;
            }

            //if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            if (!Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return;
            }

            float raidTimeElapsed = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
            float raidTimeRemaining = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();

            // Don't run the script before the raid begins
            if (raidTimeElapsed < 3)
            {
                return;
            }

            // Only find doors once per raid
            if (ToggleableDoorCount == 0)
            {
                gamePlayerOwner = FindObjectOfType<GamePlayerOwner>();
                StartCoroutine(FindAllEligibleDoors());
                return;
            }

            // Determine how many doors to toggled accounting for the raid time elapsed when the player spawns in
            doorsToToggle = (int)Math.Max(1, Math.Round(eligibleDoors.Count * ConfigController.Config.OpenDoorsDuringRaid.PercentageOfDoorsPerEvent / 100.0));
            if (!HasToggledInitialDoors)
            {
                doorsToToggle *= (int)Math.Ceiling(Math.Max(raidTimeElapsed - ConfigController.Config.OpenDoorsDuringRaid.MinRaidET, 0) / ConfigController.Config.OpenDoorsDuringRaid.TimeBetweenEvents);
                doorsToToggle = Math.Min(doorsToToggle, eligibleDoors.Count);
            }

            // Do not change doors too early or late into the raid
            if ((raidTimeElapsed < ConfigController.Config.OpenDoorsDuringRaid.MinRaidET) || (raidTimeRemaining < ConfigController.Config.OpenDoorsDuringRaid.MinRaidTimeRemaining))
            {
                if (!HasToggledInitialDoors)
                {
                    LoggingController.LogInfo("Doors cannot be opened at this time in the raid");
                    HasToggledInitialDoors = true;
                }

                return;
            }

            // Ensure there are doors to toggle
            if (doorsToToggle == 0)
            {
                return;
            }

            // Try to change the state of doors
            StartCoroutine(ToggleRandomDoors(doorsToToggle));
            updateTimer.Restart();
            doorOpeningsTimer.Start();
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsFindingDoors)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsFindingDoors, nameof(IsFindingDoors), 3000);

                IsFindingDoors = false;
            }
            if (IsTogglingDoors)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsTogglingDoors, nameof(IsTogglingDoors), 3000);

                IsTogglingDoors = false;
            }

            toggleableDoors.Clear();
            eligibleDoors.Clear();
            allowedToToggleDoor.Clear();
            updateTimer.Restart();

            HasToggledInitialDoors = false;

            IsClearing = false;
        }

        public static bool IsToggleableDoor(Door door)
        {
            return toggleableDoors.Any(d => d.Id == door.Id);
        }

        public bool ToggleDoor(Door door, EDoorState newState)
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

            // Unlock or "breach" the door if necessary
            if ((door.DoorState == EDoorState.Locked) && (newState == EDoorState.Open))
            {
                if (door.KeyId.Length > 0)
                {
                    // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                    if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningLockedDoors)
                    {
                        return true;
                    }

                    // Check if the door can be unlocked based on chance
                    System.Random randomObj = new System.Random();
                    if (randomObj.Next(0, 100) > ConfigController.Config.OpenDoorsDuringRaid.ChanceOfUnlockingDoors)
                    {
                        return false;
                    }

                    LoggingController.LogInfo("Unlocking door: " + door.Id + " (Key ID: " + door.KeyId + ")");
                }
                else
                {
                    // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                    if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningUnlockedDoors)
                    {
                        return true;
                    }

                    LoggingController.LogInfo("Preparing to breach door: " + door.Id);
                }

                door.DoorState = EDoorState.Shut;
                door.OnEnable();

                // This doesn't work
                //door.Interact(new GClass2600(EInteractionType.Unlock));
            }

            // Ignore doors that are currently being opened/closed                    
            if (!(bool)canStartInteractionMethodInfo.Invoke(door, new object[] { newState, true }))
            {
                return false;
            }

            if ((door.DoorState != EDoorState.Open) && (door.DoorState != EDoorState.Locked) && (newState == EDoorState.Open))
            {
                // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.OpeningUnlockedDoors)
                {
                    return true;
                }

                LoggingController.LogInfo("Opening door: " + door.Id);
                //door.DoorState = EDoorState.Open;
                //door.OnEnable();

                // This plays the opening noise and animation
                door.Interact(new InteractionResult(EInteractionType.Open));
                return true;
            }

            if ((door.DoorState == EDoorState.Open) && (newState == EDoorState.Shut))
            {
                // Skip the event if changes need to be limited after a certain time has elapsed after spawning
                if (shouldlimitEvents() && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.ClosingDoors)
                {
                    return true;
                }

                LoggingController.LogInfo("Closing door: " + door.Id);
                //door.DoorState = EDoorState.Open;
                //door.OnEnable();

                // This plays the opening noise and animation
                door.Interact(new InteractionResult(EInteractionType.Close));
                return true;
            }

            return false;
        }

        private IEnumerator ToggleRandomDoors(int doorsToToggle)
        {
            try
            {
                IsTogglingDoors = true;

                // Check which doors are eligible to be toggled
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(eligibleDoors.AsEnumerable(), UpdateIfDoorIsAllowedToBeToggle);
                IEnumerable<Door> doorsThatCanBeToggled = allowedToToggleDoor.Where(d => d.Value).Select(d => d.Key);

                // Toggle requested number of doors
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Repeat(doorsToToggle, ToggleRandomDoor, doorsThatCanBeToggled, ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
            }
            finally
            {
                IsTogglingDoors = false;
                HasToggledInitialDoors = true;
            }
        }

        private IEnumerator FindAllEligibleDoors()
        {
            try
            {
                IsFindingDoors = true;
                eligibleDoors.Clear();

                LoggingController.LogInfo("Searching for valid doors...");
                Door[] allNormalDoors = FindObjectsOfType<Door>();
                Door[] allKaycardDoors = FindObjectsOfType<KeycardDoor>();
                IEnumerable<Door> allDoors = allNormalDoors.Concat(allKaycardDoors);
                LoggingController.LogInfo("Searching for valid doors...found " + allDoors.Count() + " possible doors.");

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allDoors, CheckIfDoorIsEligible);

                LoggingController.LogInfo("Searching for valid doors...found " + eligibleDoors.Count + " doors.");
            }
            finally
            {
                IsFindingDoors = false;
            }
        }

        private void CheckIfDoorIsEligible(Door door)
        {
            // If the door can be toggled, add it to the dictionary
            if (!CheckIfDoorCanBeToggled(door, true))
            {
                return;
            }
            toggleableDoors.Add(door);

            // If the door is eligible for toggling during the raid, add it to the dictionary
            if (!IsEligibleDoor(door, true))
            {
                return;
            }
            eligibleDoors.Add(door);
        }

        private void UpdateIfDoorIsAllowedToBeToggle(Door door)
        {
            bool isAllowedToBeToggled = IsDoorAllowedToBeToggled(door);

            if (allowedToToggleDoor.ContainsKey(door))
            {
                allowedToToggleDoor[door] = isAllowedToBeToggled;
            }
            else
            {
                allowedToToggleDoor.Add(door, isAllowedToBeToggled);
            }
        }

        private bool IsDoorAllowedToBeToggled(Door door)
        {
            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (door.transform == null))
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

            return true;
        }

        private bool IsEligibleDoor(Door door, bool logResult = false)
        {
            // Get all items to search for key ID's
            Dictionary<string, Item> allItems = ItemHelpers.GetAllItems();

            if (door.DoorState == EDoorState.Locked)
            {
                if (allItems.ContainsKey(door.KeyId) && !ConfigController.Config.OpenDoorsDuringRaid.CanOpenLockedDoors)
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is locked and not allowed to be opened.");
                    return false;
                }

                if (door.CanBeBreached && !ConfigController.Config.OpenDoorsDuringRaid.CanBreachDoors)
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is not allowed to be breached.");
                    return false;
                }
            }

            return true;
        }

        private bool CheckIfDoorCanBeToggled(Door door, bool logResult = false)
        {
            if (!door.Operatable)
            {
                if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is inoperable.");
                return false;
            }

            if (door.gameObject.layer != InteractiveLayer)
            {
                if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is inoperable (wrong layer).");
                return false;
            }

            // Ensure there are context menu options for the door
            GClass2805 availableActions = GClass1726.GetAvailableActions(gamePlayerOwner, door);
            if ((availableActions == null) || (availableActions.Actions.Count == 0))
            {
                if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " has no interaction options.");
                return false;
            }

            // This is a sanity check but never seems to actually happen
            if (door.DoorState != EDoorState.Open && door.DoorState != EDoorState.Shut && door.DoorState != EDoorState.Locked)
            {
                if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " has an invalid state: " + door.DoorState);
                return false;
            }

            // Get all items to search for key ID's
            Dictionary<string, Item> allItems = ItemHelpers.GetAllItems();

            if (door.DoorState == EDoorState.Locked)
            {
                if (!allItems.ContainsKey(door.KeyId) && !door.CanBeBreached)
                {
                    if (logResult) LoggingController.LogInfo("Searching for valid doors...door " + door.Id + " is locked and has no valid key.");
                    return false;
                }
            }

            return true;
        }

        private void ToggleRandomDoor(IEnumerable<Door> eligibleDoors, int maxCalcTime_ms)
        {
            // Randomly sort eligible doors
            System.Random randomObj = new System.Random();
            IEnumerable<Door> randomlyOrderedKeys = eligibleDoors.OrderBy(e => randomObj.NextDouble());

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

        private static bool shouldlimitEvents()
        {
            bool shouldLimit = HasToggledInitialDoors
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (doorOpeningsTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }

    }
}
