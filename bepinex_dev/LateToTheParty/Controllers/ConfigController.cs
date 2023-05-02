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
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null;

        public static Configuration.ModConfig GetConfig()
        {
            string json = RequestHandler.GetJson("/LateToTheParty/GetConfig");
            Config = JsonConvert.DeserializeObject<Configuration.ModConfig>(json);
            return Config;
        }

        public static void SetLootMultipliers(double factor)
        {
            RequestHandler.GetJson("/LateToTheParty/SetLootMultiplier/" + factor);
        }

        public static string[] GetCarExtractNames()
        {
            string json = RequestHandler.GetJson("/LateToTheParty/GetCarExtractNames");
            string[] names = JsonConvert.DeserializeObject<string[]>(json);
            return names;
        }

        public static void ShareEscapeTime(int escapeTime, double timeRemaining)
        {
            RequestHandler.GetJson("/LateToTheParty/EscapeTime/" + escapeTime + "/" + timeRemaining);
        }
    }
}
