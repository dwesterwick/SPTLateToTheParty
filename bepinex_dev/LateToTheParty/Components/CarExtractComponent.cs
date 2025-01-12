using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Controllers;
using LateToTheParty.Helpers;
using UnityEngine;

namespace LateToTheParty.Components
{
    public class CarExtractComponent : MonoBehaviour
    {
        private ExfiltrationPoint VEXExfil = null;
        private double carLeaveTime = -1;

        private Stopwatch carExtractMonitorTimer = Stopwatch.StartNew();
        private Stopwatch carExtractPendingTimer = new Stopwatch();
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private double updateDelay = 0;

        public bool ExtractActivated => carExtractPendingTimer.IsRunning;
        public float ExtractTimeRemaining => ConfigController.Config.CarExtractDepartures.CountdownTime - (carExtractPendingTimer.ElapsedMilliseconds / 1000);

        protected void Awake()
        {
            VEXExfil = CarExtractHelpers.FindVEX();

            LoggingController.LogInfo("VEX Found: " + (VEXExfil != null));

            if (VEXExfil != null)
            {
                VEXExfil.Settings.ExfiltrationTime = ConfigController.Config.CarExtractDepartures.CountdownTime;

                setCarLeaveTime();
            }
        }

        protected void Update()
        {
            if ((VEXExfil == null) || (updateTimer.ElapsedMilliseconds < updateDelay))
            {
                return;
            }

            updateTimer.Restart();
            updateDelay = 100;

            if (!ExtractActivated && shouldlimitEvents())
            {
                return;
            }

            Player nearestPlayer = Singleton<PlayerMonitor>.Instance.GetNearestPlayer(VEXExfil.transform.position);
            if (nearestPlayer == null)
            {
                return;
            }

            float distanceToNearestPlayer = Vector3.Distance(nearestPlayer.Position, VEXExfil.transform.position);
            if (distanceToNearestPlayer < ConfigController.Config.CarExtractDepartures.ExclusionRadius)
            {
                // Wait until you're a little closer to the car to add some hysteresis
                double exclusionRadiusWithHysteresis = ConfigController.Config.CarExtractDepartures.ExclusionRadius * ConfigController.Config.CarExtractDepartures.ExclusionRadiusHysteresis;

                if (ExtractActivated && (ExtractTimeRemaining > 3) && (distanceToNearestPlayer < exclusionRadiusWithHysteresis))
                {
                    // Stop the countdown so you don't get a free ride
                    deactivateCarExfil();
                }

                return;
            }

            if (ExtractActivated)
            {
                return;
            }

            // Wait until the car should leave
            float raidTimeRemaining = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
            if (raidTimeRemaining > carLeaveTime)
            {
                return;
            }

            activateCarExfil();
        }

        private void setCarLeaveTime()
        {
            System.Random random = new System.Random();
            Configuration.MinMaxConfig leaveTimeRange = ConfigController.Config.CarExtractDepartures.RaidFractionWhenLeaving;

            if (random.Next(1, 100) <= ConfigController.Config.CarExtractDepartures.ChanceOfLeaving)
            {
                double leaveTimeFraction = leaveTimeRange.Min + ((leaveTimeRange.Max - leaveTimeRange.Min) * random.NextDouble());
                carLeaveTime = SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds * leaveTimeFraction;

                LoggingController.LogInfo("The VEX will try to leave at " + TimeSpan.FromSeconds(carLeaveTime).ToString("mm':'ss"));
            }
            else
            {
                LoggingController.LogInfo("The VEX will not leave during this raid");
            }
        }

        private void activateCarExfil()
        {
            VEXExfil.ActivateExfilForPlayer(Singleton<GameWorld>.Instance.MainPlayer);

            carExtractPendingTimer.Restart();
        }

        private void deactivateCarExfil()
        {
            VEXExfil.DeactivateExfilForPlayer(Singleton<GameWorld>.Instance.MainPlayer);
            
            // Wait a while before the car is allowed to leave again so it's less obvious that this mod is faking it
            updateDelay = ConfigController.Config.CarExtractDepartures.DelayAfterCountdownReset * 1000;

            carExtractPendingTimer.Reset();
        }

        private bool shouldlimitEvents()
        {
            bool shouldLimit = ConfigController.Config.OnlyMakeChangesJustAfterSpawning.AffectedSystems.CarDepartures
                && ConfigController.Config.OnlyMakeChangesJustAfterSpawning.Enabled
                && (carExtractMonitorTimer.ElapsedMilliseconds / 1000.0 > ConfigController.Config.OnlyMakeChangesJustAfterSpawning.TimeLimit);

            return shouldLimit;
        }
    }
}
