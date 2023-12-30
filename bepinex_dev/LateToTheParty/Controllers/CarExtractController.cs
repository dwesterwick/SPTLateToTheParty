using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class CarExtractController : MonoBehaviour
    {
        private ExfiltrationPoint VEXExfil = null;
        private double carLeaveTime = -1;
        private bool carActivated = false;
        private bool carNotPresent = false;

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                VEXExfil = null;
                carLeaveTime = -1;
                carActivated = false;
                carNotPresent = false;

                return;
            }

            if (carNotPresent || carActivated)
            {
                return;
            }

            if (!Singleton<AbstractGame>.Instance.GameTimer.Started())
            {
                return;
            }

            if (VEXExfil == null)
            {
                VEXExfil = LocationSettingsController.FindVEX();
                carNotPresent = (VEXExfil == null) || (VEXExfil?.Status == EExfiltrationStatus.NotPresent);

                LoggingController.LogInfo("VEX Found: " + !carNotPresent);
            }

            if (!carNotPresent && (carLeaveTime == -1))
            {
                System.Random random = new System.Random();
                Configuration.MinMaxConfig leaveTimeRange = ConfigController.Config.CarExtractDepartures.RaidFractionWhenLeaving;

                double leaveTimeFraction = leaveTimeRange.Min + ((leaveTimeRange.Max - leaveTimeRange.Min) * random.NextDouble());
                carLeaveTime = LocationSettingsController.CurrentLocation.EscapeTimeLimit * leaveTimeFraction * 60;

                LoggingController.LogInfo("The VEX will leave at " + TimeSpan.FromSeconds(carLeaveTime).ToString("mm':'ss"));
            }

            float raidTimeRemaining = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
            if (raidTimeRemaining > carLeaveTime)
            {
                return;
            }

            if (Vector3.Distance(Singleton<GameWorld>.Instance.MainPlayer.Position, VEXExfil.transform.position) < ConfigController.Config.CarExtractDepartures.ExclusionRadius)
            {
                return;
            }

            VEXExfil.Settings.ExfiltrationTime = ConfigController.Config.CarExtractDepartures.CountdownTime;
            LocationSettingsController.ActivateExfil(VEXExfil, Singleton<GameWorld>.Instance.MainPlayer);
            carActivated = true;
        }
    }
}
