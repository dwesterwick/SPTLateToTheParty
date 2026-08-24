using Comfort.Common;
using EFT.Airdrop;
using EFT.Interactive;
using EFT.SynchronizableObjects;
using LateToTheParty.Components;
using LateToTheParty.Helpers;
using LateToTheParty.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Patches
{
    internal class OnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when eairdropFallingStage_0=EAirdropFallingStage.Landed in ManualUpdate()
            return typeof(ClientAirDrop).GetMethod(nameof(ClientAirDrop.CheckSurface), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(AirdropSynchronizableObject ____syncObject)
        {
            // Do not run this on Fika client machines
            if (!Helpers.RaidHelpers.IsHostRaid())
            {
                return;
            }

            LootableContainer airdropContainer = ____syncObject.gameObject.GetComponentInChildren<LootableContainer>();

            string airdropType = ____syncObject.AirdropType.ToString();
            IEnumerable<EFT.InventoryLogic.Item> airdropItems = airdropContainer.ItemOwner.Items.FindAllItemsInContainers();
            Singleton<LoggingUtil>.Instance.LogInfo("Found " + airdropType + " airdrop with " + airdropItems.Count() + " items");

            Singleton<LootDestroyerComponent>.Instance.LootManager.AddLootableContainer(airdropContainer);
        }
    }
}
