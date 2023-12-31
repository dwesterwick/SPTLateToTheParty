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
using LateToTheParty.CoroutineExtensions;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class SwitchController : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool HasFoundSwitches { get; private set; } = false;
        public static bool IsTogglingSwitches { get; private set; } = false;

        private static Dictionary<EFT.Interactive.Switch, bool> hasToggledSwitch = new Dictionary<EFT.Interactive.Switch, bool>();
        private static Dictionary<EFT.Interactive.Switch, double> raidTimeRemainingToToggleSwitch = new Dictionary<EFT.Interactive.Switch, double>();

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.ToggleSwitchesDuringRaid.MaxCalcTimePerFrame);
        private static Stopwatch switchTogglingTimer = Stopwatch.StartNew();
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        public static string GetSwitchText(EFT.Interactive.Switch sw) => sw.Id + " (" + (sw.gameObject?.name ?? "???") + ")";
        public static bool CanToggleSwitch(EFT.Interactive.Switch sw) => sw.Operatable && (sw.gameObject.layer == LayerMask.NameToLayer("Interactive"));

        private void Update()
        {
            if (IsClearing)
            {
                return;
            }

            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                StartCoroutine(Clear());

                switchTogglingTimer.Restart();

                return;
            }

            if (updateTimer.ElapsedMilliseconds < ConfigController.Config.ToggleSwitchesDuringRaid.TimeBetweenEvents)
            {
                return;
            }

            updateTimer.Restart();

            // Need to wait until the raid starts or Singleton<GameWorld>.Instance.ExfiltrationController will be null
            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            {
                switchTogglingTimer.Restart();
                return;
            }

            if (!HasFoundSwitches)
            {
                findSwitches();
            }

            if (shouldlimitEvents())
            {
                //return;
            }

            if (!IsTogglingSwitches)
            {
                StartCoroutine(tryToggleAllSwitches());
            }
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsTogglingSwitches)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsTogglingSwitches, nameof(IsTogglingSwitches), 3000);

                IsTogglingSwitches = false;
            }

            HasFoundSwitches = false;
            IsTogglingSwitches = false;

            hasToggledSwitch.Clear();
            raidTimeRemainingToToggleSwitch.Clear();

            IsClearing = false;
        }

        private static void findSwitches()
        {
            System.Random random = new System.Random();
            Configuration.MinMaxConfig fractionOfSwitchesToToggleRange = ConfigController.Config.ToggleSwitchesDuringRaid.FractionOfSwitchesToToggle;
            Configuration.MinMaxConfig raidFractionWhenTogglingRange = ConfigController.Config.ToggleSwitchesDuringRaid.RaidFractionWhenToggling;

            EFT.Interactive.Switch[] allSwitches = FindObjectsOfType<EFT.Interactive.Switch>()
                .Where(s => CanToggleSwitch(s))
                .OrderBy(x => random.NextDouble())
                .ToArray();

            Configuration.MinMaxConfig switchesToToggleRange = fractionOfSwitchesToToggleRange * allSwitches.Length;
            switchesToToggleRange.Round();
            int switchesToToggle = random.Next((int)switchesToToggleRange.Min, (int)switchesToToggleRange.Max);

            for (int i = 0; i < allSwitches.Length; i++)
            {
                hasToggledSwitch.Add(allSwitches[i], false);

                double timeRemainingToToggle = -1;
                if (i < switchesToToggle)
                {
                    double timeRemainingFractionToToggle = raidFractionWhenTogglingRange.Min + ((raidFractionWhenTogglingRange.Max - raidFractionWhenTogglingRange.Min) * random.NextDouble());
                    timeRemainingToToggle = Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds * timeRemainingFractionToToggle;
                }

                if (Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints.Any(x => x.Switch == allSwitches[i]))
                {
                    LoggingController.LogInfo("Switch " + GetSwitchText(allSwitches[i]) + " is used for an extract point");

                    float maxTimeRemainingToToggle = Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds - ConfigController.Config.ToggleSwitchesDuringRaid.MinRaidETForExfilSwitches;
                    timeRemainingToToggle = Math.Min(timeRemainingToToggle, maxTimeRemainingToToggle);
                }

                raidTimeRemainingToToggleSwitch.Add(allSwitches[i], timeRemainingToToggle);
                LoggingController.LogInfo("Switch " + GetSwitchText(allSwitches[i]) + " will be toggled at " + TimeSpan.FromSeconds(timeRemainingToToggle).ToString("mm':'ss"));
            }

            HasFoundSwitches = true;
        }

        private static IEnumerator tryToggleAllSwitches()
        {
            try
            {
                IsTogglingSwitches = true;

                float raidTimeRemaining = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();

                EFT.Interactive.Switch[] remainingSwitches = hasToggledSwitch
                    .Where(s => !s.Value)
                    .Where(s => raidTimeRemaining < raidTimeRemainingToToggleSwitch[s.Key])
                    .Select(s => s.Key)
                    .ToArray();

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(remainingSwitches, tryToggleSwitch);
            }
            finally
            {
                IsTogglingSwitches = false;
            }
        }

        private static void tryToggleSwitch(EFT.Interactive.Switch sw)
        {
            if (sw.DoorState == EDoorState.Open)
            {
                if (hasToggledSwitch.ContainsKey(sw))
                {
                    hasToggledSwitch[sw] = true;
                }

                return;
            }

            if ((sw.DoorState == EDoorState.Locked) || !CanToggleSwitch(sw))
            {
                if (sw.PreviousSwitch != null)
                {
                    LoggingController.LogInfo("Switch " + GetSwitchText(sw.PreviousSwitch) + " must be toggled before switch " + sw.Id);

                    tryToggleSwitch(sw.PreviousSwitch);
                    return;
                }

                LoggingController.LogInfo("Cannot toggle switch " + GetSwitchText(sw));
            }

            Vector3? yourPosition = Singleton<GameWorld>.Instance?.MainPlayer?.Position;
            if (!yourPosition.HasValue)
            {
                return;
            }

            float distance = Vector3.Distance(yourPosition.Value, sw.transform.position);
            if (distance < ConfigController.Config.ToggleSwitchesDuringRaid.ExclusionRadius)
            {
                LoggingController.LogInfo("Switch " + GetSwitchText(sw) + " is too close to you");

                return;
            }

            LoggingController.LogInfo("Toggling switch " + GetSwitchText(sw) + "...");
            sw.Interact(new InteractionResult(EInteractionType.Open));

            if (hasToggledSwitch.ContainsKey(sw))
            {
                hasToggledSwitch[sw] = true;
            }
        }

        private static bool shouldlimitEvents()
        {
            bool shouldLimit = ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.TogglingSwitches
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (switchTogglingTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }
    }
}
