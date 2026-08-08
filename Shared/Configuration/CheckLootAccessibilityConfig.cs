using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class CheckLootAccessibilityConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "exclusion_radius", IsRequired = true)]
        public double ExclusionRadius { get; set; } = 25;

        [DataMember(Name = "max_path_search_distance", IsRequired = true)]
        public double MaxPathSearchDistance { get; set; } = 300;

        [DataMember(Name = "navmesh_search_max_distance_player", IsRequired = true)]
        public float NavmeshSearchMaxDistancePlayer { get; set; } = 10;

        [DataMember(Name = "navmesh_search_max_distance_loot", IsRequired = true)]
        public float NavmeshSearchMaxDistanceLoot { get; set; } = 2;

        [DataMember(Name = "navmesh_height_offset_complete", IsRequired = true)]
        public float NavmeshHeightOffsetComplete { get; set; } = 1.25f;

        [DataMember(Name = "navmesh_height_offset_incomplete", IsRequired = true)]
        public float NavmeshHeightOffsetIncomplete { get; set; } = 1;

        [DataMember(Name = "navmesh_obstacle_min_height", IsRequired = true)]
        public float NavmeshObstacleMinHeight { get; set; } = 0.9f;

        [DataMember(Name = "navmesh_obstacle_min_volume", IsRequired = true)]
        public float NavmeshObstacleMinVolume { get; set; } = 2;

        [DataMember(Name = "max_calc_time_per_frame_ms", IsRequired = true)]
        public double MaxCalcTimePerFrameMs { get; set; } = 4;

        [DataMember(Name = "door_obstacle_update_time", IsRequired = true)]
        public double DoorObstacleUpdateTime { get; set; } = 2;

        public CheckLootAccessibilityConfig()
        {

        }
    }
}
