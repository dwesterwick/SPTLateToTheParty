using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using DrakiaXYZ.BigBrain.Brains;
using LateToTheParty.BotLogic;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty
{
    [BepInDependency("xyz.drakia.waypoints", "1.2.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.2.0")]
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.3.1.0")]
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
                new Patches.OnGameStartedPatch().Enable();
                
                if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
                {
                    new Patches.OnItemAddedOrRemovedPatch().Enable();
                    new Patches.OnBeenKilledByAggressorPatch().Enable();
                    new Patches.OnBoxLandPatch().Enable();
                }

                if (ConfigController.Config.TraderStockChanges.Enabled)
                {
                    new Patches.QuestSetStatusPatch().Enable();
                }

                LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                this.GetOrAddComponent<DoorController>();
                this.GetOrAddComponent<NavMeshController>();
                this.GetOrAddComponent<BotQuestController>();

                if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
                {
                    this.GetOrAddComponent<LootDestroyerController>();
                }

                if (ConfigController.Config.AdjustBotSpawnChances.Enabled)
                {
                    this.GetOrAddComponent<BotConversionController>();
                    this.GetOrAddComponent<BotGenerator>();

                    List<string> botBrainsToChange = BotBrains.AllBots.ToList();
                    LoggingController.LogInfo("Loading LateToThePartyPlugin...changing bot brains: " + string.Join(", ", botBrainsToChange));

                    BrainManager.AddCustomLayer(typeof(PMCObjectiveLayer), botBrainsToChange, 25);
                }

                if (ConfigController.Config.Debug.Enabled)
                {
                    new Patches.DoorInteractionPatch().Enable();
                    new Patches.BotDoorInteractionPatch().Enable();
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
