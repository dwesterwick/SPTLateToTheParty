using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class DestroyLootDuringRaidConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "exclusion_radius", IsRequired = true)]
        public double ExclusionRadius { get; set; } = 40;

        [DataMember(Name = "exclusion_radius_bots", IsRequired = true)]
        public double ExclusionRadiusBots { get; set; } = 25;

        [DataMember(Name = "nearby_interactive_object_search_distance", IsRequired = true)]
        public float NearbyInteractiveObjectSearchDistance { get; set; } = 0.75f;

        [DataMember(Name = "only_search_for_nearby_trunks", IsRequired = true)]
        public bool OnlySearchForNearbyTrunks { get; set; } = true;

        [DataMember(Name = "avg_slots_per_player", IsRequired = true)]
        public uint AvgSlotsPerPlayer { get; set; } = 60;

        [DataMember(Name = "players_with_loot_factor_for_maps_without_pscavs", IsRequired = true)]
        public double PlayersWithLootFactorForMapsWithoutPscavs { get; set; } = 0.3;

        [DataMember(Name = "min_loot_age", IsRequired = true)]
        public double MinLootAge { get; set; } = 120;

        [DataMember(Name = "destruction_event_limits", IsRequired = true)]
        public DestructionEventLimits DestructionEventLimits { get; set; } = new DestructionEventLimits();

        [DataMember(Name = "map_traversal_speed_mps", IsRequired = true)]
        public double MapTraversalSpeedMps { get; set; } = 2;

        [DataMember(Name = "min_distance_traveled_for_update", IsRequired = true)]
        public double MinDistanceTraveledForUpdate { get; set; } = 1;

        [DataMember(Name = "min_time_before_update_ms", IsRequired = true)]
        public double MinTimeBeforeUpdateMs { get; set; } = 30;

        [DataMember(Name = "max_time_before_update_ms", IsRequired = true)]
        public double MaxTimeBeforeUpdateMs { get; set; } = 5000;

        [DataMember(Name = "max_calc_time_per_frame_ms", IsRequired = true)]
        public double MaxCalcTimePerFrameMs { get; set; } = 5;

        [DataMember(Name = "max_time_without_destroying_any_loot", IsRequired = true)]
        public double MaxTimeWithoutDestroyingAnyLoot { get; set; } = 60;

        [DataMember(Name = "ignore_items_dropped_by_player", IsRequired = true)]
        public IgnoreItemsDroppedByPlayer IgnoreItemsDroppedByPlayer { get; set; } = new IgnoreItemsDroppedByPlayer();

        [DataMember(Name = "ignore_items_on_dead_bots", IsRequired = true)]
        public IgnoreItemsOnDeadBots IgnoreItemsOnDeadBots { get; set; } = new IgnoreItemsOnDeadBots();

        [DataMember(Name = "excluded_parents", IsRequired = true)]
        public string[] ExcludedParents { get; set; } = Array.Empty<string>();

        [DataMember(Name = "check_loot_accessibility", IsRequired = true)]
        public CheckLootAccessibilityConfig CheckLootAccessibility { get; set; } = new CheckLootAccessibilityConfig();

        [DataMember(Name = "loot_ranking", IsRequired = true)]
        public LootRankingConfig LootRanking { get; set; } = new LootRankingConfig();

        public DestroyLootDuringRaidConfig()
        {

        }
    }
}
