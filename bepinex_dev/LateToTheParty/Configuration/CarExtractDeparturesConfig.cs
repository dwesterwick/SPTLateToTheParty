using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class CarExtractDeparturesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("countdown_time")]
        public float CountdownTime { get; set; } = 60;

        [JsonProperty("exclusion_radius")]
        public double ExclusionRadius { get; set; } = 150;

        [JsonProperty("raid_fraction_when_leaving")]
        public MinMaxConfig RaidFractionWhenLeaving { get; set; } = new MinMaxConfig();

        public CarExtractDeparturesConfig()
        {

        }
    }
}
