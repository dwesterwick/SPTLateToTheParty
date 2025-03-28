﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Common.Http;
using Newtonsoft.Json;

namespace LateToTheParty.Controllers
{
    public static class ConfigController
    {
        public static string LoggingPath { get; private set; } = null;

        public static Configuration.ModConfig Config { get; private set; } = null;
        public static Configuration.LootRankingWeightingConfig LootRanking { get; private set; } = null;
        public static string ModPathRelative { get; } = "/BepInEx/plugins/DanW-LateToTheParty";

        public static Configuration.ModConfig GetConfig()
        {
            string errorMessage = "!!!!! Cannot retrieve config.json data from the server. The mod will not work properly! !!!!!";
            string json = RequestHandler.GetJson("/LateToTheParty/GetConfig");

            TryDeserializeObject(json, errorMessage, out Configuration.ModConfig _config);
            Config = _config;

            return Config;
        }

        public static string GetLoggingPath()
        {
            if (LoggingPath != null)
            {
                return LoggingPath;
            }

            LoggingPath = AppDomain.CurrentDomain.BaseDirectory + ModPathRelative + "/log/";

            return LoggingPath;
        }

        public static Configuration.LootRankingWeightingConfig GetLootRankingData()
        {
            string errorMessage = "Cannot read loot ranking data from the server. Falling back to using random loot ranking.";
            string json = RequestHandler.GetJson("/LateToTheParty/GetLootRankingData");

            bool rankingDataIsValid = TryDeserializeObject(json, errorMessage, out Configuration.LootRankingWeightingConfig _lootRanking);
            LootRanking = _lootRanking;

            if (LootRanking.Items.Any(i => !i.Value.Value.HasValue))
            {
                rankingDataIsValid = false;
                LoggingController.LogErrorToServerConsole("The loot ranking data is invalid. Loot ranking may not work correctly!");
            }

            if (!rankingDataIsValid)
            {
                LoggingController.WriteLogFile("json_invalidLootRankingData", "json", json);
            }

            return LootRanking;
        }

        public static void SetLootMultipliers(double factor)
        {
            RequestHandler.GetJson("/LateToTheParty/SetLootMultiplier/" + factor);
        }

        public static string[] GetCarExtractNames()
        {
            string errorMessage = "Cannot read car-extract names from the server. VEX extract chances will not be modified.";
            string json = RequestHandler.GetJson("/LateToTheParty/GetCarExtractNames");

            TryDeserializeObject(json, errorMessage, out string[] _names);
            return _names;
        }

        public static void ReportInfoToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Info("Late to the Party", message);
        }

        public static void ReportWarningToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Warn("Late to the Party", message);
        }

        public static void ReportErrorToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Error("Late to the Party", message);
        }

        public static bool TryDeserializeObject<T>(string json, string errorMessage, out T obj)
        {
            try
            {
                if (json.Length == 0)
                {
                    throw new InvalidCastException("Could deserialize an empty string to an object of type " + typeof(T).FullName);
                }

                obj = JsonConvert.DeserializeObject<T>(json, GClass1629.SerializerSettings);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);
                LoggingController.LogErrorToServerConsole(errorMessage);
            }

            obj = default(T);
            if (obj == null)
            {
                obj = (T)Activator.CreateInstance(typeof(T));
            }

            return false;
        }

        public static double InterpolateForFirstCol(double[][] array, double value)
        {
            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            if (value <= array[0][0])
            {
                return array[0][1];
            }

            for (int i = 1; i < array.Length; i++)
            {
                if (array[i][0] >= value)
                {
                    if (array[i][0] - array[i - 1][0] == 0)
                    {
                        return array[i][1];
                    }

                    return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
                }
            }

            return array.Last()[1];
        }
    }
}
