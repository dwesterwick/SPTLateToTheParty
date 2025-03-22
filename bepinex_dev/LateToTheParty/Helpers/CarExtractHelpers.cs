using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using LateToTheParty.Controllers;

namespace LateToTheParty.Helpers
{
    public static class CarExtractHelpers
    {
        private static string[] carExtractNames = new string[0];

        public static bool IsCarExtract(string extractName)
        {
            getCarExtractNames();

            return carExtractNames.Contains(extractName);
        }

        public static ExfiltrationPoint FindVEX()
        {
            if (Singleton<GameWorld>.Instance?.ExfiltrationController?.ExfiltrationPoints == null)
            {
                return null;
            }

            return FindVEX(Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints);
        }

        public static ExfiltrationPoint FindVEX(this IEnumerable<ExfiltrationPoint> allExfils)
        {
            getCarExtractNames();

            foreach (ExfiltrationPoint exfil in allExfils)
            {
                if (exfil.Status == EExfiltrationStatus.NotPresent)
                {
                    continue;
                }

                if (carExtractNames.Contains(exfil.Settings.Name))
                {
                    return exfil;
                }
            }

            return null;
        }

        public static void ActivateExfilForPlayer(this ExfiltrationPoint exfil, IPlayer player)
        {
            // Needed to start the car extract
            exfil.OnItemTransferred(player);

            // Copied from the end of ExfiltrationPoint.Proceed()
            if (exfil.Status == EExfiltrationStatus.UncompleteRequirements)
            {
                switch (exfil.Settings.ExfiltrationType)
                {
                    case EExfiltrationType.Individual:
                        exfil.SetStatusLogged(EExfiltrationStatus.RegularMode, "Proceed-3");
                        break;
                    case EExfiltrationType.SharedTimer:
                        exfil.SetStatusLogged(EExfiltrationStatus.Countdown, "Proceed-1");
                        break;
                    case EExfiltrationType.Manual:
                        exfil.SetStatusLogged(EExfiltrationStatus.AwaitsManualActivation, "Proceed-2");
                        break;
                }
            }

            LoggingController.LogInfo("Extract " + exfil.Settings.Name + " activated for player " + player.Profile.Nickname);
        }

        public static void DeactivateExfilForPlayer(this ExfiltrationPoint exfil, IPlayer player)
        {
            exfil.method_2(player);
            LoggingController.LogInfo("Extract " + exfil.Settings.Name + " deactivated for player " + player.Profile.Nickname);
        }

        private static void getCarExtractNames()
        {
            if (carExtractNames.Length == 0)
            {
                LoggingController.Logger.LogInfo("Getting car extract names...");
                carExtractNames = ConfigController.GetCarExtractNames();
            }
        }
    }
}
