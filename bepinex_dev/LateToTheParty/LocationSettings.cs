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
        public float TrainMaxTime { get; set; } = 1500;
        public float TrainMinTime { get; set; } = 1200;
        public int TrainWaitTime { get; set; } = 420;
        public float VExChance { get; set; } = 50;

        public LocationSettings()
        {

        }

        public LocationSettings(int escapeTimeLimit)
        {
            EscapeTimeLimit = escapeTimeLimit;
        }
    }
}
