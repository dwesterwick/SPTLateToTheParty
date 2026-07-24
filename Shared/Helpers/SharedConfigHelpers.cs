using LateToTheParty.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Helpers
{
    public static class SharedConfigHelpers
    {
        public static bool IsModEnabled(this ModConfig? modConfig) => modConfig?.Enabled == true;
        public static bool IsDebugEnabled(this ModConfig? modConfig) => modConfig?.Debug?.Enabled == true;

        public static void DisableMod(this ModConfig modConfig) => modConfig.Enabled = false;
    }
}
