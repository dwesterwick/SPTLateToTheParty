using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class CarExtractDeparturesConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "countdown_time", IsRequired = true)]
        public float CountdownTime { get; set; } = 60;

        [DataMember(Name = "delay_after_countdown_reset", IsRequired = true)]
        public double DelayAfterCountdownReset { get; set; } = 120;

        [DataMember(Name = "exclusion_radius", IsRequired = true)]
        public double ExclusionRadius { get; set; } = 150;

        [DataMember(Name = "exclusion_radius_hysteresis", IsRequired = true)]
        public double ExclusionRadiusHysteresis { get; set; } = 0.9;

        [DataMember(Name = "chance_of_leaving", IsRequired = true)]
        public double ChanceOfLeaving { get; set; } = 50;

        [DataMember(Name = "raid_fraction_when_leaving", IsRequired = true)]
        public MinMaxConfig RaidFractionWhenLeaving { get; set; } = new MinMaxConfig(0.3, 0.8);

        public CarExtractDeparturesConfig()
        {

        }
    }
}
