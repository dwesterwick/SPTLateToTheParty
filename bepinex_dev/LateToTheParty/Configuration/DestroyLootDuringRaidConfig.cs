using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class DestroyLootDuringRaidConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 1;

        [JsonProperty("map_traversal_speed_mps")]
        public double MapTraversalSpeed { get; set; } = 2;

        [JsonProperty("min_distance_traveled_for_update")]
        public double MinDistanceTraveledForUpdate { get; set; } = 1;

        [JsonProperty("min_time_before_update_ms")]
        public double MinTimeBeforeUpdate { get; set; } = 30;

        [JsonProperty("max_time_before_update_ms")]
        public double MaxTimeBeforeUpdate { get; set; } = 5000;

        [JsonProperty("max_calc_time_per_frame_ms")]
        public double MaxCalcTimePerFrame { get; set; } = 5;

        [JsonProperty("ignore_items_dropped_by_player")]
        public IgnoreItemsDroppedByPlayerConfig IgnoreItemsDroppedByPlayer { get; set; } = new IgnoreItemsDroppedByPlayerConfig();

        [JsonProperty("excluded_parents")]
        public string[] ExcludedParents { get; set; } = new string[0];

        public DestroyLootDuringRaidConfig()
        {

        }
    }
}
