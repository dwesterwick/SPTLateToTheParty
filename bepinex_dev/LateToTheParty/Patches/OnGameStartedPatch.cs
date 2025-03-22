using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using LateToTheParty.Components;
using LateToTheParty.Controllers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class OnGameStartedPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(GameWorld __instance)
        {
            Controllers.LocationSettingsController.HasRaidStarted = true;

            addComponents();
        }

        private static void addComponents()
        {
            Controllers.LoggingController.LogInfo("Adding components...");

            GameObject gameWorld = Singleton<GameWorld>.Instance.gameObject;

            Singleton<PlayerMonitor>.Create(gameWorld.GetOrAddComponent<PlayerMonitor>());
            Singleton<DoorTogglingComponent>.Create(gameWorld.GetOrAddComponent<DoorTogglingComponent>());
            Singleton<SwitchTogglingComponent>.Create(gameWorld.GetOrAddComponent<SwitchTogglingComponent>());
            
            if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
            {
                Singleton<LootDestroyerComponent>.Create(gameWorld.GetOrAddComponent<LootDestroyerComponent>());
            }

            if (ConfigController.Config.CarExtractDepartures.Enabled)
            {
                Singleton<CarExtractComponent>.Create(gameWorld.GetOrAddComponent<CarExtractComponent>());
            }

            if (ConfigController.Config.Debug.Enabled)
            {
                Singleton<PathRenderer>.Create(gameWorld.GetOrAddComponent<PathRenderer>());
            }
        }
    }
}
