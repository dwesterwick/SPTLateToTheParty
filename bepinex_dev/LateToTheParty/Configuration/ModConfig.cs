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
        public bool Enabled { get; set; } = true;

        [JsonProperty("debug")]
        public bool Debug { get; set; } = false;

        [JsonProperty("adjust_bot_waves")]
        public bool AdjustBotWaves { get; set; } = false;

        [JsonProperty("scav")]
        public EscapeTimeConfig Scav { get; set; } = new EscapeTimeConfig();

        [JsonProperty("pmc")]
        public EscapeTimeConfig PMC { get; set; } = new EscapeTimeConfig();

        [JsonProperty("destroy_loot_during_raid")]
        public DestroyLootDuringRaidConfig DestroyLootDuringRaid { get; set; } = new DestroyLootDuringRaidConfig();

        [JsonProperty("loot_multipliers")]
        public double[][] LootMultipliers { get; set; } = new double[0][];

        [JsonProperty("vex_chance_reduction")]
        public double[][] VExChanceReductions { get; set; } = new double[0][];

        public ModConfig()
        {

        }
    }
}
