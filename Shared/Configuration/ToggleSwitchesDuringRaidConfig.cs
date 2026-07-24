using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ToggleSwitchesDuringRaidConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "time_between_events_ms", IsRequired = true)]
        public double TimeBetweenEventsMs { get; set; } = 3000;

        [DataMember(Name = "exclusion_radius", IsRequired = true)]
        public double ExclusionRadius { get; set; } = 75;

        [DataMember(Name = "min_raid_ET_for_exfil_switches", IsRequired = true)]
        public double MinRaidEtForExfilSwitches { get; set; } = 600;

        [DataMember(Name = "delay_after_pressing_prereq_switch_s_per_m", IsRequired = true)]
        public double DelayAfterPressingPrereqSwitchSPerM { get; set; } = 1;

        [DataMember(Name = "raid_fraction_when_toggling", IsRequired = true)]
        public MinMaxConfig RaidFractionWhenToggling { get; set; } = new MinMaxConfig(0.1, 0.95);

        [DataMember(Name = "fraction_of_switches_to_toggle", IsRequired = true)]
        public MinMaxConfig FractionOfSwitchesToToggle { get; set; } = new MinMaxConfig(0.2, 0.7);

        [DataMember(Name = "max_calc_time_per_frame_ms", IsRequired = true)]
        public double MaxCalcTimePerFrameMs { get; set; } = 3;

        public ToggleSwitchesDuringRaidConfig()
        {

        }
    }
}
