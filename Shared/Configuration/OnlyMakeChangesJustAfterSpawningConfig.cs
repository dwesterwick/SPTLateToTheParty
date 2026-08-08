using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class OnlyMakeChangesJustAfterSpawningConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "time_limit", IsRequired = true)]
        public double TimeLimit { get; set; } = 5;

        [DataMember(Name = "affected_systems", IsRequired = true)]
        public AffectedSystemsConfig AffectedSystems { get; set; } = new AffectedSystemsConfig();

        public OnlyMakeChangesJustAfterSpawningConfig()
        {

        }
    }
}
