using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    public class ConfigEditorInfoConfig
    {
        [JsonProperty("description")]
        public string Description { get; set; } = "";

        public ConfigEditorInfoConfig()
        {

        }
    }
}
