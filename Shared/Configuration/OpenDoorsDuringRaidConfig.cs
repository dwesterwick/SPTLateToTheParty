using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class OpenDoorsDuringRaidConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "can_open_locked_doors", IsRequired = true)]
        public bool CanOpenLockedDoors { get; set; } = true;

        [DataMember(Name = "can_breach_doors", IsRequired = true)]
        public bool CanBreachDoors { get; set; } = true;

        [DataMember(Name = "exclusion_radius", IsRequired = true)]
        public double ExclusionRadius { get; set; } = 40;

        [DataMember(Name = "min_raid_ET", IsRequired = true)]
        public double MinRaidET { get; set; } = 180;

        [DataMember(Name = "min_raid_time_remaining", IsRequired = true)]
        public double MinRaidTimeRemaining { get; set; } = 300;

        [DataMember(Name = "time_between_door_events", IsRequired = true)]
        public double TimeBetweenDoorEvents { get; set; } = 60;

        [DataMember(Name = "percentage_of_doors_per_event", IsRequired = true)]
        public double PercentageOfDoorsPerEvent { get; set; } = 3;

        [DataMember(Name = "chance_of_unlocking_doors", IsRequired = true)]
        public double ChanceOfUnlockingDoors { get; set; } = 50;

        [DataMember(Name = "chance_of_closing_doors", IsRequired = true)]
        public double ChanceOfClosingDoors { get; set; } = 15;

        [DataMember(Name = "max_calc_time_per_frame_ms", IsRequired = true)]
        public float MaxCalcTimePerFrameMs { get; set; } = 3;

        public OpenDoorsDuringRaidConfig()
        {

        }
    }
}
