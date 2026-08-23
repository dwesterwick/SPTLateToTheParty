using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LoggingUtil(ISptLogger<LateToTheParty_Server> logger)
    {
        public void Debug(string message)
        {
            logger.Debug(GetLogPrefix() + message);
        }

        public void Info(string message)
        {
            logger.Info(GetLogPrefix() + message);
        }

        public void Warning(string message)
        {
            logger.Warning(GetLogPrefix() + message);
        }

        public void Error(string message)
        {
            logger.Error(GetLogPrefix() + message);
        }

        private string GetLogPrefix()
        {
            return $"[{ModInfo.MODNAME}] ";
        }
    }
}