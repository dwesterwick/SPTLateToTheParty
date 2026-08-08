using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class AffectedSystemsConfig
    {
        [DataMember(Name = "loot_destruction", IsRequired = true)]
        public bool LootDestruction { get; set; } = true;

        [DataMember(Name = "opening_unlocked_doors", IsRequired = true)]
        public bool OpeningUnlockedDoors { get; set; } = true;

        [DataMember(Name = "opening_locked_doors", IsRequired = true)]
        public bool OpeningLockedDoors { get; set; } = true;

        [DataMember(Name = "closing_doors", IsRequired = true)]
        public bool ClosingDoors { get; set; } = true;

        [DataMember(Name = "car_departures", IsRequired = true)]
        public bool CarDepartures { get; set; } = true;

        [DataMember(Name = "toggling_switches", IsRequired = true)]
        public bool TogglingSwitches { get; set; } = true;

        public AffectedSystemsConfig()
        {

        }
    }
}
