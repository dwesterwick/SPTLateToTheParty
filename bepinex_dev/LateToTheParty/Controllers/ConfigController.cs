using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;

namespace LateToTheParty.Controllers
{
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null;
        public static Configuration.LootRankingWeightingConfig LootRanking { get; private set; } = null;

        public static Configuration.ModConfig GetConfig()
        {
            string json = RequestHandler.GetJson("/LateToTheParty/GetConfig");
            Config = JsonConvert.DeserializeObject<Configuration.ModConfig>(json);
            return Config;
        }

        public static string GetLoggingPath()
        {
            string json = RequestHandler.GetJson("/LateToTheParty/GetLoggingPath");
            Configuration.LoggingPath path = JsonConvert.DeserializeObject<Configuration.LoggingPath>(json);
            return path.Path;
        }

        public static Configuration.LootRankingWeightingConfig GetLootRankingData()
        {
            string errorMessage = "Cannot read loot ranking data from the server. Falling back to using random loot ranking.";
            string json = RequestHandler.GetJson("/LateToTheParty/GetLootRankingData");

            TryDeserializeObject(json, errorMessage, out Configuration.LootRankingWeightingConfig _lootRanking);
            LootRanking = _lootRanking;

            return LootRanking;
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

        public static void ShareQuestStatusChange(string questID, string newStatus)
        {
            RequestHandler.GetJson("/LateToTheParty/QuestStatusChange/" + questID + "/" + newStatus);
        }

        public static void ReportError(string errorMessage)
        {
            RequestHandler.GetJson("/LateToTheParty/ReportError/" + errorMessage);
        }

        public static bool TryDeserializeObject<T>(string json, string errorMessage, out T obj)
        {
            try
            {
                if (json.Length == 0)
                {
                    throw new InvalidCastException("Could deserialize an empty string to an object of type " + typeof(T).FullName);
                }

                obj = JsonConvert.DeserializeObject<T>(json);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);
                LoggingController.LogErrorToServerConsole(errorMessage);
                obj = default(T);
            }

            return false;
        }
    }
}
