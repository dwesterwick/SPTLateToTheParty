using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class FenceAssortChangesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("always_regenerate")]
        public bool AlwaysRegenerate { get; set; } = false;

        [JsonProperty("assort_size")]
        public int AssortSize { get; set; } = 190;

        [JsonProperty("assort_size_discount")]
        public int AssortSizeDiscount { get; set; } = 90;

        [JsonProperty("min_allowed_item_value")]
        public double MinAllowedItemValue { get; set; } = 20000;

        [JsonProperty("itemTypeLimits_Override")]
        public Dictionary<string, int> ItemTypeLimitsOverride { get; set; } = new Dictionary<string, int>();

        public FenceAssortChangesConfig()
        {

        }
    }
}
