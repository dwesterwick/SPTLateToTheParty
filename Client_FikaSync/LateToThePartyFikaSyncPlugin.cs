using BepInEx;
using Comfort.Common;
using LateToTheParty.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty
{
    [BepInPlugin(ModInfo.GUID + "fikasync", ModInfo.MODNAME + "FikaSync", ModInfo.MOD_VERSION)]
    internal class LateToThePartyFikaSyncPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyFikaSync...");

            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            Singleton<LoggingUtil>.Instance.LogInfo("Loading LateToThePartyFikaSync...done.");
        }
    }
}
