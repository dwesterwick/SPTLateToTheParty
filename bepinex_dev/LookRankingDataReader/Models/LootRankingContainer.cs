using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LookRankingDataReader.Models
{
    internal class LootRankingContainer
    {
        [JsonProperty("costPerSlot")]
        public double CostPerSlot { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("maxDim")]
        public double MaxDim { get; set; }

        [JsonProperty("items")]
        public Dictionary<string, LootRankingData> Items { get; set; } = new Dictionary<string, LootRankingData>();

        public LootRankingContainer()
        {

        }
    }
}
