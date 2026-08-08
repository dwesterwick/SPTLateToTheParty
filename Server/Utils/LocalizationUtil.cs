using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LocalizationUtil
    {
        private ServerLocalisationService _serverLocalisationService;

        public LocalizationUtil(ServerLocalisationService serverLocalisationService)
        {
            _serverLocalisationService = serverLocalisationService;
        }

        public string GetLocalizedName(TemplateItem item)
        {
            return _serverLocalisationService.GetLocalisedValue($"{item.Id} Name");
        }

        public string GetLocalizedName(Trader trader)
        {
            return _serverLocalisationService.GetLocalisedValue($"{trader.Base.Id} Nickname");
        }
    }
}
