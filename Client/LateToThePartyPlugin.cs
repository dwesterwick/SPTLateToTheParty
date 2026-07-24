using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using LateToTheParty.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty
{
    [BepInPlugin(ModInfo.GUID, ModInfo.MODNAME, ModInfo.MOD_VERSION)]
    internal class LateToThePartyPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Logger.LogInfo("Loading LateToTheParty...");

            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            if (ConfigUtil.CurrentConfig.Enabled)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Loading LateToTheParty...enabled");

                AddConfigOptions(Config);
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading LateToTheParty...done.");
        }

        private void AddConfigOptions(ConfigFile Config)
        {
            
        }
    }
}
