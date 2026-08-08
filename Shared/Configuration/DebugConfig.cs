using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class DebugConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "loot_path_visualization", IsRequired = true)]
        public LootPathVisualizationConfig LootPathVisualization { get; set; } = new LootPathVisualizationConfig();

        public DebugConfig()
        {

        }
    }
}
