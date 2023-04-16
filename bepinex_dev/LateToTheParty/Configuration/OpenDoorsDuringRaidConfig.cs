using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class OpenDoorsDuringRaidConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 1;

        [JsonProperty("min_raid_ET")]
        public double MinRaidET { get; set; } = 60;

        [JsonProperty("min_raid_time_remaining")]
        public double MinRaidTimeRemaining { get; set; } = 600;

        [JsonProperty("percent_doors_to_open")]
        public double PercentDoorsToOpen { get; set; } = 25;

        public OpenDoorsDuringRaidConfig()
        {

        }
    }
}
