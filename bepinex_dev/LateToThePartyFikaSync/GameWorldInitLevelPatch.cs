using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Coop.Utils;
using LateToTheParty;
using SPT.Reflection.Patching;

namespace LateToThePartyFikaSync
{
    internal class GameWorldInitLevelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("InitLevel", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix()
        {
            if (FikaBackendUtils.IsServer)
            {
                return;
            }

            Logger.LogWarning("Disabling LateToTheParty plugin for client machine...");

            LateToThePartyPlugin.Disable();
        }
    }
}
