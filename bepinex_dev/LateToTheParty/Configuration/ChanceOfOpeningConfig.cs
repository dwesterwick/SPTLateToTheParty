using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class ChanceOfOpeningConfig
    {
        [JsonProperty("unlocked")]
        public float Unlocked { get; set; } = 100;

        [JsonProperty("locked")]
        public float Locked { get; set; } = 50;

        public ChanceOfOpeningConfig()
        {

        }
    }
}
