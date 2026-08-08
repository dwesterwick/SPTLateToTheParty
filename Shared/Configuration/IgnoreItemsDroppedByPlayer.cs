using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class IgnoreItemsDroppedByPlayer
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "only_items_brought_into_raid", IsRequired = true)]
        public bool OnlyItemsBroughtIntoRaid { get; set; } = false;

        public IgnoreItemsDroppedByPlayer()
        {

        }
    }
}
