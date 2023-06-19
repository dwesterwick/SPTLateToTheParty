using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class LootPathVisualizationConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        public LootPathVisualizationConfig()
        {

        }
    }
}
