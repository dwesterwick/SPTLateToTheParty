using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class LootRankingWeightingConfig
    {
        [DataMember(Name = "default_inventory_id", IsRequired = true)]
        public string DefaultInventoryId { get; set; } = "55d7217a4bdc2d86028b456d";

        [DataMember(Name = "cost_per_slot", IsRequired = true)]
        public double CostPerSlot { get; set; } = 0.001;

        [DataMember(Name = "weight", IsRequired = true)]
        public double Weight { get; set; } = -0.5;

        [DataMember(Name = "size", IsRequired = true)]
        public double Size { get; set; } = -1;

        [DataMember(Name = "gridSize", IsRequired = true)]
        public double GridSize { get; set; } = 1.3;

        [DataMember(Name = "max_dim", IsRequired = true)]
        public double MaxDim { get; set; } = -1;

        [DataMember(Name = "armor_class", IsRequired = true)]
        public double ArmorClass { get; set; } = 10;

        [DataMember(Name = "parents", IsRequired = true)]
        public Dictionary<string, NameValueConfig> Parents { get; set; } = new Dictionary<string, NameValueConfig>();

        public LootRankingWeightingConfig()
        {

        }
    }
}
