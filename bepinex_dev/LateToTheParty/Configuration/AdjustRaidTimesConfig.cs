using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class AdjustRaidTimesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("scav")]
        public EscapeTimeConfig Scav { get; set; } = new EscapeTimeConfig();

        [JsonProperty("pmc")]
        public EscapeTimeConfig PMC { get; set; } = new EscapeTimeConfig();

        [JsonProperty("adjust_vex_chance")]
        public bool AdjustVexChance { get; set; } = true;

        [JsonProperty("adjust_bot_waves")]
        public bool AdjustBotWaves { get; set; } = true;

        [JsonProperty("can_reduce_starting_loot")]
        public bool CanReduceStartingLoot { get; set; } = true;

        public AdjustRaidTimesConfig()
        {

        }
    }
}
