using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class BotConversionController : MonoBehaviour
    {
        private static bool EscapeTimeShared = false;

        private void Update()
        {
            if (EscapeTimeShared)
            {
                return;
            }

            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                EscapeTimeShared = false;

                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            int totalEscapeTime = LocationSettingsController.LastOriginalEscapeTime * 60;
            float escapeTimeSec = GClass1426.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = totalEscapeTime - escapeTimeSec;

            // Don't run the script before the raid begins
            if (raidTimeElapsed < 3)
            {
                return;
            }

            ConfigController.ShareEscapeTime(totalEscapeTime, escapeTimeSec);
            EscapeTimeShared = true;
        }
    }
}
