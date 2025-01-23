using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class CanModifyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InteractionsHandlerClass).GetMethod("CanModifyItem", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(Item item)
        {


            return true;
        }
    }
}
