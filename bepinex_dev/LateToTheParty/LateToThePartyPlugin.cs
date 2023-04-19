using System;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using LateToTheParty.Controllers;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.1.6.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Logger.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;

            if (ConfigController.Config.Enabled)
            {
                LoggingController.Logger.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
                new Patches.ShowScreenPatch().Enable();
                new Patches.OnItemAddedOrRemovedPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();

                LoggingController.Logger.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                this.GetOrAddComponent<LootDestroyerController>();
                this.GetOrAddComponent<DoorController>();
            }

            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }
    }
}
