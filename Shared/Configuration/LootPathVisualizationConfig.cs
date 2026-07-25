using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class LootPathVisualizationConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "points_per_circle", IsRequired = true)]
        public byte PointsPerCircle { get; set; } = 10;

        [DataMember(Name = "outline_loot", IsRequired = true)]
        public bool OutlineLoot { get; set; } = false;

        [DataMember(Name = "loot_outline_radius", IsRequired = true)]
        public float LootOutlineRadius { get; set; } = 0.1f;

        [DataMember(Name = "only_outline_loot_with_pathing", IsRequired = true)]
        public bool OnlyOutlineLootWithPathing { get; set; } = false;

        [DataMember(Name = "draw_incomplete_paths", IsRequired = true)]
        public bool DrawIncompletePaths { get; set; } = false;

        [DataMember(Name = "draw_complete_paths", IsRequired = true)]
        public bool DrawCompletePaths { get; set; } = true;

        [DataMember(Name = "outline_obstacles", IsRequired = true)]
        public bool OutlineObstacles { get; set; } = false;

        [DataMember(Name = "only_outline_filtered_obstacles", IsRequired = true)]
        public bool OnlyOutlineFilteredObstacles { get; set; } = true;

        [DataMember(Name = "show_obstacle_collision_points", IsRequired = true)]
        public bool ShowObstacleCollisionPoints { get; set; } = true;

        [DataMember(Name = "collision_point_radius", IsRequired = true)]
        public float CollisionPointRadius { get; set; } = 0.05f;

        [DataMember(Name = "show_door_obstacles", IsRequired = true)]
        public bool ShowDoorObstacles { get; set; } = true;

        [DataMember(Name = "door_obstacle_min_radius", IsRequired = true)]
        public double DoorObstacleMinRadius { get; set; } = 0.3;

        public LootPathVisualizationConfig()
        {

        }
    }
}
