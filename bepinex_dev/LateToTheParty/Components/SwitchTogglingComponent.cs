using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Controllers;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Helpers;
using UnityEngine;

namespace LateToTheParty.Components
{
    public class SwitchTogglingComponent : MonoBehaviour
    {
        public bool IsTogglingSwitches { get; private set; } = false;
        public bool HasToggledInitialSwitches { get; private set; } = false;

        private Dictionary<EFT.Interactive.Switch, bool> hasToggledSwitch = new Dictionary<EFT.Interactive.Switch, bool>();
        private Dictionary<EFT.Interactive.Switch, double> raidTimeRemainingToToggleSwitch = new Dictionary<EFT.Interactive.Switch, double>();

        private EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.ToggleSwitchesDuringRaid.MaxCalcTimePerFrame);
        private Stopwatch switchTogglingTimer = Stopwatch.StartNew();
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private System.Random staticRandomGen = new System.Random();

        protected void Awake()
        {
            if (!ConfigController.Config.ToggleSwitchesDuringRaid.Enabled)
            {
                HasToggledInitialSwitches = true;

                return;
            }

            try
            {
                findSwitches();
            }
            catch (Exception)
            {
                // If findSwitches() fails for some reason, HasToggledInitialSwitches must be set to true or all sounds from WorldInteractiveObjects will
                // be suppressed by WorldInteractiveObjectPlaySoundPatch
                HasToggledInitialSwitches = true;

                throw;
            }
        }

        protected void Update()
        {
            if (!ConfigController.Config.ToggleSwitchesDuringRaid.Enabled)
            {
                return;
            }

            if (updateTimer.ElapsedMilliseconds < ConfigController.Config.ToggleSwitchesDuringRaid.TimeBetweenEvents)
            {
                return;
            }
            updateTimer.Restart();

            if (!HasToggledInitialSwitches && shouldlimitEvents())
            {
                return;
            }

            if (!IsTogglingSwitches)
            {
                StartCoroutine(tryToggleAllSwitches());
            }
        }

        private void findSwitches()
        {
            // Randomly sort all switches that players can toggle
            EFT.Interactive.Switch[] allSwitches = FindObjectsOfType<EFT.Interactive.Switch>()
                .Where(s => s.CanToggle())
                .OrderBy(x => staticRandomGen.NextDouble())
                .ToArray();

            // Select a random number of total switches to toggle throughout the raid
            Configuration.MinMaxConfig fractionOfSwitchesToToggleRange = ConfigController.Config.ToggleSwitchesDuringRaid.FractionOfSwitchesToToggle;
            Configuration.MinMaxConfig switchesToToggleRange = fractionOfSwitchesToToggleRange * allSwitches.Length;
            switchesToToggleRange.Round();
            int switchesToToggle = staticRandomGen.Next((int)switchesToToggleRange.Min, (int)switchesToToggleRange.Max);

            for (int i = 0; i < allSwitches.Length; i++)
            {
                hasToggledSwitch.Add(allSwitches[i], false);
                setTimeToToggleSwitch(allSwitches[i], 0, i >= switchesToToggle);
            }
        }

        private void setTimeToToggleSwitch(EFT.Interactive.Switch sw, float minTimeFromNow = 0, bool neverToggle = false)
        {
            // Select a random time during the raid to toggle the switch
            Configuration.MinMaxConfig raidFractionWhenTogglingRange = ConfigController.Config.ToggleSwitchesDuringRaid.RaidFractionWhenToggling;
            double timeRemainingToToggle = -1;
            if (!neverToggle)
            {
                double timeRemainingFractionToToggle = raidFractionWhenTogglingRange.Min + ((raidFractionWhenTogglingRange.Max - raidFractionWhenTogglingRange.Min) * staticRandomGen.NextDouble());
                timeRemainingToToggle = SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds * timeRemainingFractionToToggle;
            }

            // If the switch controls an extract point (i.e. the Labs cargo elevator), don't toggle it until after a certain time
            if (Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints.Any(x => x.Switch == sw))
            {
                LoggingController.LogInfo("Switch " + sw.GetText() + " is used for an extract point");

                float maxTimeRemainingToToggle = SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds - ConfigController.Config.ToggleSwitchesDuringRaid.MinRaidETForExfilSwitches;
                timeRemainingToToggle = Math.Min(timeRemainingToToggle, maxTimeRemainingToToggle);
            }

            // If needed, cap the minimum time into the raid when the switch will be toggled
            if (minTimeFromNow > 0)
            {
                float raidTimeRemaining = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
                float maxTimeRemainingToToggle = raidTimeRemaining - minTimeFromNow;

                timeRemainingToToggle = Math.Min(timeRemainingToToggle, maxTimeRemainingToToggle);
            }

            if (raidTimeRemainingToToggleSwitch.ContainsKey(sw))
            {
                raidTimeRemainingToToggleSwitch[sw] = timeRemainingToToggle;
            }
            else
            {
                raidTimeRemainingToToggleSwitch.Add(sw, timeRemainingToToggle);
            }
            LoggingController.LogInfo("Switch " + sw.GetText() + " will be toggled at " + TimeSpan.FromSeconds(timeRemainingToToggle).ToString("mm':'ss"));
        }

        private IEnumerator tryToggleAllSwitches()
        {
            try
            {
                IsTogglingSwitches = true;

                float raidTimeRemaining = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();

                // Enumerate all switches that haven't been toggled yet but should
                EFT.Interactive.Switch[] remainingSwitches = hasToggledSwitch
                    .Where(s => !s.Value)
                    .Where(s => raidTimeRemaining < raidTimeRemainingToToggleSwitch[s.Key])
                    .Select(s => s.Key)
                    .ToArray();

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(remainingSwitches, tryToggleSwitch);

                // Add a delay before setting HasToggledInitialSwitches to true to make sure doors have power before they're (possibly) toggled
                yield return new WaitForSeconds(1);
            }
            finally
            {
                IsTogglingSwitches = false;
                HasToggledInitialSwitches = true;
            }
        }

        private void tryToggleSwitch(EFT.Interactive.Switch sw)
        {
            if (sw.DoorState == EDoorState.Interacting)
            {
                //LoggingController.LogInfo("Somebody is already interacting with switch " + GetSwitchText(sw));

                return;
            }

            if (sw.DoorState == EDoorState.Open)
            {
                if (hasToggledSwitch.ContainsKey(sw))
                {
                    hasToggledSwitch[sw] = true;
                }

                return;
            }

            if ((sw.DoorState == EDoorState.Locked) || !sw.CanToggle())
            {
                // Check if another switch needs to be toggled first before this one is available
                if (sw.PreviousSwitch != null)
                {
                    LoggingController.LogInfo("Switch " + sw.PreviousSwitch.GetText() + " must be toggled before switch " + sw.Id);

                    tryToggleSwitch(sw.PreviousSwitch);

                    // If this is beginning of a Scav raid, toggle the switch immediately after the prerequisite switch. Otherwise, add a minimum delay. 
                    if (HasToggledInitialSwitches)
                    {
                        float delayBeforeSwitchCanBeToggled = InteractiveObjectHelpers.GetSwitchTogglingDelayTime(sw, sw.PreviousSwitch);
                        //LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " cannot be toggled for another " + delayBeforeSwitchCanBeToggled + "s");

                        setTimeToToggleSwitch(sw, delayBeforeSwitchCanBeToggled, false);

                        return;
                    }
                }
                else
                {
                    LoggingController.LogWarning("Cannot toggle switch " + sw.GetText());
                }
            }

            // Check if the switch is too close to a human player to toggle
            Player nearestPlayer = Singleton<PlayerMonitor>.Instance.GetNearestPlayer(sw.transform.position);
            if (nearestPlayer == null)
            {
                LoggingController.LogWarning("Cannot find an alive player near the switch " + sw.Id);
                return;
            }

            float distance = Vector3.Distance(nearestPlayer.Position, sw.transform.position);
            if (distance < ConfigController.Config.ToggleSwitchesDuringRaid.ExclusionRadius)
            {
                //LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " is too close to a human player");

                return;
            }

            sw.StartExecuteInteraction(new InteractionResult(EInteractionType.Open));

            if (hasToggledSwitch.ContainsKey(sw))
            {
                hasToggledSwitch[sw] = true;
            }
        }

        private bool shouldlimitEvents()
        {
            bool shouldLimit = ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.TogglingSwitches
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (switchTogglingTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }
    }
}
