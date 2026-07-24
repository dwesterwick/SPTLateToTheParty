using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class LootRankingConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "randomness", IsRequired = true)]
        public double Randomness { get; set; } = 200;

        [DataMember(Name = "top_value_retain_count", IsRequired = true)]
        public double TopValueRetainCount { get; set; } = 5;

        [DataMember(Name = "always_regenerate", IsRequired = true)]
        public bool AlwaysRegenerate { get; set; } = true;

        [DataMember(Name = "child_item_limits", IsRequired = true)]
        public ChildItemLimitsConfig ChildItemLimits { get; set; } = new ChildItemLimitsConfig();

        [DataMember(Name = "weighting", IsRequired = true)]
        public WeightingConfig Weighting { get; set; } = new WeightingConfig();

        public LootRankingConfig()
        {

        }
    }
}
