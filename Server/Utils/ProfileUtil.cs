using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ProfileUtil
    {
        private ProfileHelper _profileHelper;

        public ProfileUtil(ProfileHelper profileHelper)
        {
            _profileHelper = profileHelper;
        }

        public PmcData? GetPmcProfile(MongoId sessionId) => _profileHelper.GetPmcProfile(sessionId);
        public PmcData? GetScavProfile(MongoId sessionId) => _profileHelper.GetScavProfile(sessionId);
    }
}
