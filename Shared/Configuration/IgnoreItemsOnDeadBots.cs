using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class IgnoreItemsOnDeadBots
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "only_if_you_killed_them", IsRequired = true)]
        public bool OnlyIfYouKilledThem { get; set; } = true;

        public IgnoreItemsOnDeadBots()
        {

        }
    }
}
