using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class CheckLootAccessibilityConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        public CheckLootAccessibilityConfig()
        {

        }
    }
}
