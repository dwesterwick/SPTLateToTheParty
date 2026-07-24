using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class AdjustBotSpawnChancesConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "adjust_bosses", IsRequired = true)]
        public bool AdjustBosses { get; set; } = true;

        [DataMember(Name = "excluded_bosses", IsRequired = true)]
        public string[] ExcludedBosses { get; set; } = Array.Empty<string>();

        public AdjustBotSpawnChancesConfig()
        {

        }
    }
}
