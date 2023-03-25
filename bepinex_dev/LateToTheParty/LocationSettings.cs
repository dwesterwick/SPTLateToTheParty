using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty
{
    public class LocationSettings
    {
        public int EscapeTimeLimit { get; set; }

        public LocationSettings()
        {

        }

        public LocationSettings(int escapeTimeLimit)
        {
            EscapeTimeLimit = escapeTimeLimit;
        }
    }
}
