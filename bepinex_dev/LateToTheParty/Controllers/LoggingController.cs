﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using LateToTheParty.Models;

namespace LateToTheParty.Controllers
{
    public static class LoggingController
    {
        public static ManualLogSource Logger { get; set; } = null;

        private static LoggingBuffer loggingBuffer;

        public static void InitializeLoggingBuffer(int length, string path, string filePrefix)
        {
            loggingBuffer = new LoggingBuffer(length, path, filePrefix);
        }

        public static void LogInfo(string message)
        {
            if (!ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogInfo(message);
            loggingBuffer.AddMessage(GetMessagePrefix('I') +  message);
        }

        public static void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogWarning(message);
            loggingBuffer.AddMessage(GetMessagePrefix('W') + message);
        }

        public static void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug)
            {
                return;
            }

            Logger.LogError(message);
            loggingBuffer.AddMessage(GetMessagePrefix('E') + message);
        }

        public static void WriteMessagesToLogFile()
        {
            loggingBuffer.WriteMessagesToLogFile();
        }

        private static string GetMessagePrefix(char messageType)
        {
            return "[" + messageType + "] " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": ";
        }
    }
}
