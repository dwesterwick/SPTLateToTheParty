using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;
using LateToTheParty.Controllers;
using LateToTheParty.Helpers;
using EFT;
using LateToTheParty.Components;

namespace LateToTheParty.Patches
{
    internal class OnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when eairdropFallingStage_0=EAirdropFallingStage.Landed in ManualUpdate()
            return typeof(AirdropLogicClass).GetMethod("method_13", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(AirdropSynchronizableObject ___airdropSynchronizableObject_0)
        {
            LootableContainer airdropContainer = ___airdropSynchronizableObject_0.gameObject.GetComponentInChildren<LootableContainer>();

            string airdropType = ___airdropSynchronizableObject_0.AirdropType.ToString();
            IEnumerable<EFT.InventoryLogic.Item> airdropItems = airdropContainer.ItemOwner.Items.FindAllItemsInContainers();
            LoggingController.LogInfo("Found " + airdropType + " airdrop with " + airdropItems.Count() + " items");

            Singleton<LootDestroyerComponent>.Instance.LootManager.AddLootableContainer(airdropContainer);
        }
    }
}
