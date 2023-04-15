using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace LateToTheParty.Controllers
{
    public static class LoggingController
    {
        public static ManualLogSource Logger { get; set; } = null;

        public static void LogInfo(string message)
        {
            if (!ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogInfo(message);
        }

        public static void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogWarning(message);
        }

        public static void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogError(message);
        }
    }
}
