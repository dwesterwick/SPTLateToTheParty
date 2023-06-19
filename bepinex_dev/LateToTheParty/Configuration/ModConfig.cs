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
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [JsonProperty("adjust_raid_times")]
        public AdjustRaidTimesConfig AdjustRaidTimes { get; set; } = new AdjustRaidTimesConfig();

        [JsonProperty("destroy_loot_during_raid")]
        public DestroyLootDuringRaidConfig DestroyLootDuringRaid { get; set; } = new DestroyLootDuringRaidConfig();

        [JsonProperty("open_doors_during_raid")]
        public OpenDoorsDuringRaidConfig OpenDoorsDuringRaid { get; set; } = new OpenDoorsDuringRaidConfig();

        [JsonProperty("adjust_bot_spawn_chances")]
        public AdjustBotSpawnChancesConfig AdjustBotSpawnChances { get; set; } = new AdjustBotSpawnChancesConfig();

        [JsonProperty("loot_multipliers")]
        public double[][] LootMultipliers { get; set; } = new double[0][];

        [JsonProperty("vex_chance_reduction")]
        public double[][] VExChanceReductions { get; set; } = new double[0][];

        [JsonProperty("pmc_spawn_chance_multipliers")]
        public double[][] PMCSpawnChanceMultipliers { get; set; } = new double[0][];

        [JsonProperty("boss_spawn_chance_multipliers")]
        public double[][] BossSpawnChanceMultipliers { get; set; } = new double[0][];

        public ModConfig()
        {

        }
    }
}
