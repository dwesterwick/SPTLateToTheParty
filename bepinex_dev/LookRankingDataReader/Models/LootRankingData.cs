﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LookRankingDataReader.Models
{
    internal class LootRankingData
    {
        [JsonProperty("id")]
        public string ID { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("costPerSlot")]
        public double CostPerSlot { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("maxDim")]
        public double MaxDim { get; set; }

        public LootRankingData()
        {

        }
    }
}
