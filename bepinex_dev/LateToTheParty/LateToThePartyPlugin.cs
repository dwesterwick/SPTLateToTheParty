using System;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using LateToTheParty.Controllers;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.1.15.0")]
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
                string loggingPath = ConfigController.GetLoggingPath();
                LoggingController.InitializeLoggingBuffer(200, loggingPath, this.Info.Metadata.Name);

                LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnItemAddedOrRemovedPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.OnBoxLandPatch().Enable();

                LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                this.GetOrAddComponent<LootDestroyerController>();
                this.GetOrAddComponent<DoorController>();
                this.GetOrAddComponent<BotConversionController>();
                this.GetOrAddComponent<NavMeshController>();

                if (ConfigController.Config.Debug)
                {
                    this.GetOrAddComponent<PathRender>();
                    AppDomain.CurrentDomain.UnhandledException += LogAndThrowUnhandledException;
                }
            }

            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }

        private void LogAndThrowUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            LoggingController.LogError("[ UNHANDLED EXCEPTION - PLEASE RESTART THE GAME ASAP ]");
            LoggingController.LogError(ex.ToString());

            LoggingController.WriteMessagesToLogFile();

            throw ex;
        }
    }
}
