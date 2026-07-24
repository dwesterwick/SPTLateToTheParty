using System.Runtime.Serialization;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ModConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "debug", IsRequired = true)]
        public bool Debug { get; set; } = false;

        public ModConfig()
        {

        }
    }
}
