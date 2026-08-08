using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using LateToTheParty.Components;
using LateToTheParty.Utils;
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
            Singleton<LoggingUtil>.Instance.LogInfo("Adding components...");

            GameObject gameWorld = Singleton<GameWorld>.Instance.gameObject;

            Singleton<PlayerMonitor>.Create(gameWorld.GetOrAddComponent<PlayerMonitor>());
            Singleton<DoorTogglingComponent>.Create(gameWorld.GetOrAddComponent<DoorTogglingComponent>());
            Singleton<SwitchTogglingComponent>.Create(gameWorld.GetOrAddComponent<SwitchTogglingComponent>());
            
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.Enabled)
            {
                Singleton<LootDestroyerComponent>.Create(gameWorld.GetOrAddComponent<LootDestroyerComponent>());
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.CarExtractDepartures.Enabled)
            {
                Singleton<CarExtractComponent>.Create(gameWorld.GetOrAddComponent<CarExtractComponent>());
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.Enabled)
            {
                Singleton<PathRenderer>.Create(gameWorld.GetOrAddComponent<PathRenderer>());
            }
        }
    }
}
