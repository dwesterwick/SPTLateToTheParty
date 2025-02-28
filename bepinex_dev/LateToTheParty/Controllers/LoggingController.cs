using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Controllers
{
    public static class LoggingController
    {
        public static BepInEx.Logging.ManualLogSource Logger { get; set; } = null;
        public static string LoggingPath { get; private set; } = "";

        public static void SetLoggingPath(string path)
        {
            LoggingPath = path;
        }

        public static void LogDebug(string message)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogDebug(message);
        }

        public static void LogInfo(string message)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogInfo(message);
        }

        public static void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogWarning(message);
        }

        public static void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogError(message);
        }

        public static void LogWarningToServerConsole(string message)
        {
            LogWarning(message);
            ConfigController.ReportWarningToServer(message);
        }

        public static void LogErrorToServerConsole(string message)
        {
            LogError(message);
            ConfigController.ReportErrorToServer(message);
        }

        public static void WriteLogFile(string filenamePrefix, string fileExtension, string content)
        {
            string filename = LoggingPath
                + filenamePrefix
                + "_"
                + DateTime.Now.ToFileTimeUtc()
                + "." + fileExtension;

            try
            {
                if (!Directory.Exists(LoggingPath))
                {
                    Directory.CreateDirectory(LoggingPath);
                }

                File.WriteAllText(filename, content);

                LogInfo("Writing " + filenamePrefix + " log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LogError("Writing " + filenamePrefix + " log file...failed!");
                LogError(e.ToString());
            }
        }

        public static void WriteLootLogFile(Dictionary<Item, Models.LootInfo.AbstractLootInfo> lootInfo, string currentLocationName)
        {
            string filenamePrefix = "loot_" + currentLocationName.Replace(" ", "");

            LogInfo("Writing " + filenamePrefix + " log file...");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Item,Template ID,Value,Raid ET When Found,Raid ET When Destroyed,Accessible");
            foreach (Item item in lootInfo.Keys)
            {
                sb.Append(item.LocalizedName().Replace(",", "") + ",");
                sb.Append(item.TemplateId + ",");
                sb.Append(ConfigController.LootRanking.Items[item.TemplateId].Value + ",");
                sb.Append((lootInfo[item].RaidETWhenFound.HasValue ? lootInfo[item].RaidETWhenFound : 0) + ",");
                sb.Append(lootInfo[item].RaidETWhenDestroyed.HasValue ? lootInfo[item].RaidETWhenDestroyed.ToString() : "");
                sb.AppendLine("," + lootInfo[item].PathData.IsAccessible.ToString());
            }

            WriteLogFile(filenamePrefix, "csv", sb.ToString());
        }
    }
}
