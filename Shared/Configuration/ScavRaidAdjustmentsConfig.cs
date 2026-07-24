using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ScavRaidAdjustmentsConfig
    {
        [DataMember(Name = "always_spawn_late", IsRequired = true)]
        public bool AlwaysSpawnLate { get; set; } = true;

        public ScavRaidAdjustmentsConfig()
        {

        }
    }
}
