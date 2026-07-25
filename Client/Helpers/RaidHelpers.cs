using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;

namespace LateToTheParty.Helpers
{
    public static class RaidHelpers
    {
        public static bool IsHostRaid() => Singleton<IBotGame>.Instantiated && Singleton<IBotGame>.Instance.BotsController?.IsEnable == true;
        public static bool IsInHideout() => Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance.LocationId == "hideout";
    }
}
