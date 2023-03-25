using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aki.Common.Http;
using UnityEngine;
using Newtonsoft.Json;

namespace LateToTheParty.Controllers
{
    public class ConfigController : MonoBehaviour
    {
        public static Configuration.ModConfig GetConfig()
        {
            string json = RequestHandler.GetJson("/LateToTheParty/GetConfig");
            Configuration.ModConfig config = JsonConvert.DeserializeObject<Configuration.ModConfig>(json);
            return config;
        }
    }
}
