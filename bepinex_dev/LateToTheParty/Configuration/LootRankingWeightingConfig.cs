using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootRankingWeightingConfig
    {
        [JsonProperty("costPerSlot")]
        public double CostPerSlot { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("maxDim")]
        public double MaxDim { get; set; }

        [JsonProperty("parents")]
        public Dictionary<string, LootRankingForParentConfig> Parents { get; set; } = new Dictionary<string, LootRankingForParentConfig>();

        [JsonProperty("items")]
        public Dictionary<string, LootRankingItemDataConfig> Items { get; set; } = new Dictionary<string, LootRankingItemDataConfig>();

        public LootRankingWeightingConfig()
        {

        }
    }
}
