using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class WeaponSearchUtil
    {
        private LoggingUtil _loggingUtil;
        private ConfigUtil _configUtil;
        private DatabaseService _databaseService;

        public WeaponSearchUtil(LoggingUtil loggingUtil, ConfigUtil configUtil, DatabaseService databaseService)
        {
            _loggingUtil = loggingUtil;
            _configUtil = configUtil;
            _databaseService = databaseService;
        }

        public void FindBestWeaponMatchFromTraders(TemplateItem item)
        {

        }
    }
}
