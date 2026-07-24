using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace LateToTheParty.Helpers
{
    public static class DatabaseHelpers
    {
        public static Location GetAndVerifyLocation(this DatabaseService databaseService, string locationId)
        {
            Location? location = databaseService.GetLocation(locationId);
            if (location == null)
            {
                throw new InvalidOperationException($"Cannot find location \"${locationId}\" in database.");
            }

            return location;
        }

        public static IEnumerable<Location> EnumerateLocations(this DatabaseService databaseService)
        {
            return databaseService.GetLocations().GetDictionary().Values;
        }

        public static IEnumerable<string> EnumerateLocationIDs(this DatabaseService databaseService)
        {
            return databaseService.GetLocations().GetDictionary().Keys;
        }

        public static bool Contains(this IEnumerable<string> ids, MongoId id) => ids.Any(id.Equals);
    }
}
