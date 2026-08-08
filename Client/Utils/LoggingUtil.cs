using Comfort.Common;
using EFT.InventoryLogic;
using LateToTheParty.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LateToTheParty.Utils
{
    internal class LoggingUtil
    {
        public const string MOD_RELATIVE_PATH = "/BepInEx/plugins/LateToTheParty";

        private BepInEx.Logging.ManualLogSource _logger;

        private string _loggingPath = null!;
        public string LoggingPath
        {
            get
            {
                if (_loggingPath == null)
                {
                    _loggingPath = GetLoggingPath();
                }

                return _loggingPath;
            }
        }

        private string GetLoggingPath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + MOD_RELATIVE_PATH + "/log/";
            LogInfo("Logging path: " + path);

            return path;
        }

        public LoggingUtil(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogDebug(message);
        }

        public void LogInfo(string message, bool alwaysShow = false)
        {
            if (!alwaysShow && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogInfo(message);
        }

        public void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogWarning(message);
        }

        public void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogError(message);
        }

        public void LogDebugToServerConsole(string message)
        {
            LogDebug(message);
            SPT.Common.Utils.ServerLog.Debug(ModInfo.MODNAME, message);
        }

        public void LogInfoToServerConsole(string message)
        {
            LogInfo(message);
            SPT.Common.Utils.ServerLog.Info(ModInfo.MODNAME, message);
        }

        public void LogWarningToServerConsole(string message)
        {
            LogWarning(message);
            SPT.Common.Utils.ServerLog.Warn(ModInfo.MODNAME, message);
        }

        public void LogErrorToServerConsole(string message)
        {
            LogError(message);
            SPT.Common.Utils.ServerLog.Error(ModInfo.MODNAME, message);
        }

        public void CreateLogFile(string logName, string filename, string content)
        {
            try
            {
                if (!Directory.Exists(Singleton<LoggingUtil>.Instance.LoggingPath))
                {
                    Directory.CreateDirectory(Singleton<LoggingUtil>.Instance.LoggingPath);
                }

                File.WriteAllText(filename, content);

                LogDebug("Writing " + logName + " log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LogError("Writing " + logName + " log file...failed!");
                LogError(e.ToString());
            }
        }

        public void WriteLootLogFile(Dictionary<Item, Models.LootInfo.AbstractLootInfo> lootInfo, string currentLocationName)
        {
            string filenamePrefix = "loot_" + currentLocationName.Replace(" ", "");

            LogInfo("Writing " + filenamePrefix + " log file...");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Item,Template ID,Value,Raid ET When Found,Raid ET When Destroyed,Accessible");
            foreach (Item item in lootInfo.Keys)
            {
                sb.Append(item.LocalizedName().Replace(",", "") + ",");
                sb.Append(item.TemplateId + ",");
                sb.Append(Singleton<ConfigUtil>.Instance.LootRanking[item.TemplateId].Value + ",");
                sb.Append((lootInfo[item].RaidETWhenFound.HasValue ? lootInfo[item].RaidETWhenFound : 0) + ",");
                sb.Append(lootInfo[item].RaidETWhenDestroyed.HasValue ? lootInfo[item].RaidETWhenDestroyed.ToString() : "");
                sb.AppendLine("," + lootInfo[item].PathData.IsAccessible.ToString());
            }

            CreateLogFile(filenamePrefix, "csv", sb.ToString());
        }
    }
}