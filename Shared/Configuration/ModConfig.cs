using System.Runtime.Serialization;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ModConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "debug", IsRequired = true)]
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [DataMember(Name = "scav_raid_adjustments", IsRequired = true)]
        public ScavRaidAdjustmentsConfig ScavRaidAdjustments { get; set; } = new ScavRaidAdjustmentsConfig();

        [DataMember(Name = "car_extract_departures", IsRequired = true)]
        public CarExtractDeparturesConfig CarExtractDepartures { get; set; } = new CarExtractDeparturesConfig();

        [DataMember(Name = "adjust_bot_spawn_chances", IsRequired = true)]
        public AdjustBotSpawnChancesConfig AdjustBotSpawnChances { get; set; } = new AdjustBotSpawnChancesConfig();

        [DataMember(Name = "only_make_changes_just_after_spawning", IsRequired = true)]
        public OnlyMakeChangesJustAfterSpawningConfig OnlyMakeChangesJustAfterSpawning { get; set; } = new OnlyMakeChangesJustAfterSpawningConfig();

        [DataMember(Name = "destroy_loot_during_raid", IsRequired = true)]
        public DestroyLootDuringRaidConfig DestroyLootDuringRaid { get; set; } = new DestroyLootDuringRaidConfig();

        [DataMember(Name = "open_doors_during_raid", IsRequired = true)]
        public OpenDoorsDuringRaidConfig OpenDoorsDuringRaid { get; set; } = new OpenDoorsDuringRaidConfig();

        [DataMember(Name = "toggle_switches_during_raid", IsRequired = true)]
        public ToggleSwitchesDuringRaidConfig ToggleSwitchesDuringRaid { get; set; } = new ToggleSwitchesDuringRaidConfig();

        [DataMember(Name = "loot_multipliers", IsRequired = true)]
        public double[][] LootMultipliers { get; set; } = new double[0][];

        [DataMember(Name = "fraction_of_players_full_of_loot", IsRequired = true)]
        public double[][] FractionOfPlayersFullOfLoot { get; set; } = new double[0][];

        [DataMember(Name = "boss_spawn_chance_multipliers", IsRequired = true)]
        public double[][] BossSpawnChanceMultipliers { get; set; } = new double[0][];

        public ModConfig()
        {

        }
    }
}
