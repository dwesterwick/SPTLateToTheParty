using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ChildItemLimitsConfig
    {
        [DataMember(Name = "count", IsRequired = true)]
        public byte Count { get; set; } = 5;

        [DataMember(Name = "total_weight", IsRequired = true)]
        public double TotalWeight { get; set; } = 8;

        public ChildItemLimitsConfig()
        {

        }
    }
}
