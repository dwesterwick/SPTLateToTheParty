using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class NameValueConfig
    {
        [DataMember(Name = "name", IsRequired = true)]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name = "value", IsRequired = true)]
        public double Value { get; set; } = 0;

        public NameValueConfig()
        {

        }
    }
}
