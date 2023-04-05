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

        [JsonProperty("min_distance_traveled_for_update")]
        public double MinDistanceTraveledForUpdate { get; set; } = 1;

        [JsonProperty("excluded_parents")]
        public string[] ExcludedParents { get; set; } = new string[0];

        public DestroyLootDuringRaidConfig()
        {

        }
    }
}
