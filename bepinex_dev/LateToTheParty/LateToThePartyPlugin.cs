﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using LateToTheParty.Controllers;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "2.7.0.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        protected void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Logger.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;
            ModName = Info.Metadata.Name;

            if (!confirmNoPreviousVersionExists())
            {
                Chainloader.DependencyErrors.Add("An older version of " + ModName + " still exists in '/BepInEx/plugins'. Please remove LateToTheParty.dll from that directory, or this mod will not work correctly.");
                return;
            }

            if (ConfigController.Config.Enabled)
            {
                string loggingPath = ConfigController.GetLoggingPath();
                LoggingController.InitializeLoggingBuffer(200, loggingPath, this.Info.Metadata.Name);

                LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
                new Patches.StartLocalGamePatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                
                if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
                {
                    new Patches.OnItemAddedOrRemovedPatch().Enable();
                    new Patches.OnBeenKilledByAggressorPatch().Enable();
                    new Patches.OnBoxLandPatch().Enable();
                }

                LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                this.GetOrAddComponent<InteractiveObjectController>();
                this.GetOrAddComponent<NavMeshController>();
                this.GetOrAddComponent<PlayerMonitorController>();

                if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
                {
                    this.GetOrAddComponent<LootDestroyerController>();
                }

                if (ConfigController.Config.AdjustBotSpawnChances.Enabled && ConfigController.Config.AdjustBotSpawnChances.AdjustPMCConversionChances)
                {
                    this.GetOrAddComponent<BotConversionController>();
                }

                if (ConfigController.Config.CarExtractDepartures.Enabled)
                {
                    this.GetOrAddComponent<CarExtractController>();
                }

                if (ConfigController.Config.ToggleSwitchesDuringRaid.Enabled)
                {
                    this.GetOrAddComponent<SwitchController>();
                    new Patches.WorldInteractiveObjectPlaySoundPatch().Enable();
                }

                if (ConfigController.Config.Debug.Enabled)
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

        private bool confirmNoPreviousVersionExists()
        {
            string oldPath = AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/LateToTheParty.dll";
            if (File.Exists(oldPath))
            {
                return false;
            }

            return true;
        }
    }
}
