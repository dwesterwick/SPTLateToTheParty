using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    internal class OnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropSynchronizableObject).GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(AirdropSynchronizableObject __instance)
        {
            LootableContainer container = __instance.gameObject.GetComponentInChildren<LootableContainer>();
            if (container == null)
            {
                throw new InvalidOperationException("Cannot find the airdop's LootableContainer");
            }

            LootManager.AddLootableContainer(container);
        }
    }
}
