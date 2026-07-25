using LateToTheParty.Helpers;
using LookRankingDataReader.Models;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Utils
{
    internal class ConfigUtil
    {
        private Configuration.ModConfig? _currentConfig;
        public Configuration.ModConfig CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    GetConfig();
                }

                return _currentConfig!;
            }
        }

        private void GetConfig()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetConfig");

            string json = RequestHandler.GetJson(routeName);
            Configuration.ModConfig? configResponse = JsonConvert.DeserializeObject<Configuration.ModConfig>(json);
            if (configResponse == null)
            {
                throw new InvalidOperationException("Could not deserialize config file");
            }

            _currentConfig = configResponse;
        }

        private Dictionary<string, LootRankingDataConfig>? _lootRanking;
        public Dictionary<string, LootRankingDataConfig> LootRanking
        {
            get
            {
                if (_lootRanking == null)
                {
                    GetLootRankingData();
                }

                return _lootRanking!;
            }
        }

        private void GetLootRankingData()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetLootRankingData");

            string json = RequestHandler.GetJson(routeName);
            Dictionary<string, LootRankingDataConfig>? response = JsonConvert.DeserializeObject<Dictionary<string, LootRankingDataConfig>>(json);
            if (response == null)
            {
                throw new InvalidOperationException("Could not deserialize loot ranking data");
            }

            _lootRanking = response;
        }

        private string[]? _carExtractNames;
        public string[] CarExtractNames
        {
            get
            {
                if (_carExtractNames == null)
                {
                    GetCarExtractNames();
                }

                return _carExtractNames!;
            }
        }

        private void GetCarExtractNames()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetLootRankingData");

            string json = RequestHandler.GetJson(routeName);
            string[]? response = JsonConvert.DeserializeObject<string[]>(json);
            if (response == null)
            {
                throw new InvalidOperationException("Could not deserialize car extract names");
            }

            _carExtractNames = response;
        }

        public static void SetLootMultipliers(double factor)
        {
            string routeName = SharedRouterHelpers.GetRoutePath("SetLootMultiplier");
            string json = RequestHandler.GetJson(routeName + "/" + factor);
        }
    }
}
