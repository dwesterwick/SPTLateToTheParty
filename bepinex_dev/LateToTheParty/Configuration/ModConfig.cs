using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LateToTheParty.Configuration
{
    public class ModConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("debug")]
        public bool Debug { get; set; }

        [JsonProperty("scav")]
        public EscapeTimeConfig Scav { get; set; } = new EscapeTimeConfig();

        [JsonProperty("pmc")]
        public EscapeTimeConfig PMC { get; set; } = new EscapeTimeConfig();

        [JsonProperty("loot_multipliers")]
        public double[][] LootMultipliers { get; set; } = new double[0][];

        public ModConfig()
        {

        }
    }
}
