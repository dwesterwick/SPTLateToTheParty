using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class DestructionEventLimits
    {
        [DataMember(Name = "rate", IsRequired = true)]
        public double Rate { get; set; } = 1;

        [DataMember(Name = "items", IsRequired = true)]
        public uint Items { get; set; } = 30;

        [DataMember(Name = "slots", IsRequired = true)]
        public uint Slots { get; set; } = 50;

        public DestructionEventLimits()
        {

        }
    }
}
