Make your SPT experience closer to live Tarkov with loot disappearing and doors opening throughout the raid. You will now spawn in late as a Scav, PMC's are more likely to spawn early in the raid, traders can sell out of items, and much more!

This mod makes the the following changes to your SPT experience:
* Loot (including on dead bots) will be gradually removed throughout the raid to simulate other players taking it. 
* Doors will randomly open and close throughout the raid to simulate players moving through the map (thanks to help from DrakiaXYZ!). If you're lucky, locked doors may be opened for you...
* Compared to vanilla SPT, PMC's are more likely to spawn toward the beginning of the raid and less likely to spawn toward the end of it. 
* Your Scav will spawn into the raid with an unknown amount of time remaining in it. However, that also means you may be able to run directly to extract and get a "Survived" status if enough time has passed in the raid. There is also a small chance that your PMC will spawn into the raid slightly late. 
* If you spawn into the map late as a Scav, bosses are less likely to spawn. 
* If you spawn into the map late as a Scav, vehicle extracts are less likely to be available. 
* Trader stock reduces over time (until it resets), and desirable items (like MP-133's) can sell out
* Fence sells more items, including ammo, but most items are significantly less valuable than what he sells in the base game

This mod is highly customizable by modifying the *config.json* file. Here are the settings you can change:

* **debug.enabled**: Enable debug mode.
* **debug.scav_cooldown_time**: Cooldown timer (in seconds) after a Scav raid ends before you're allowed to start another one. This is **1500** by default, which is the same as the base game.
* **debug.free_labs_access**: If **true**, Labs cards are no longer required to enter Labs. 
* **debug.min_level_for_flea**: The minimum player level to be able to access the flea market. 
* **debug.trader_resupply_time_factor**: A multiplier for trader reset times. For example, if this is 0.5, trader resets occur every 30 min instead of 60 min. **trader_stock_changes.max_ammo_buy_rate** and **trader_stock_changes.max_item_buy_rate** are divided by this value for testing purposes. 
* **debug.loot_path_visualization.enabled**: Enable visualization of loot items and NavMesh pathing to them to view which ones the mod thinks are accessible. Most visuals require **destroy_loot_during_raid.check_loot_accessibility.enabled=true**.
* **debug.loot_path_visualization.points_per_circle**: The number of points to use for drawing a circle. This is **10** by default. Considering this is only used for debugging, higher values might result in rounder-looking circles, but there isn't much benefit.
* **debug.loot_path_visualization.outline_loot**: Draw a spherical outline around loot items and loot containers that are not empty. The center of the sphere should be in the center of loose loot, but it will be somewhere around the perimeter of loot containers (and varies by the type of container). The color of the outline will change based on the loot's accessibility as determined by the mod. Green = accessible, Red = inaccessible, White = undetermined (cannot find a valid NavMesh path to the loot). 
* **debug.loot_path_visualization.loot_outline_radius**: The radius (in meters) for the circles used in **debug.loot_path_visualization.outline_loot**.
* **debug.loot_path_visualization.only_outline_loot_with_pathing**: Only draw outlines for loot that requires the mod to determine its accessibility by finding a NavMesh path to it. Many items will be excluded because their accessibility is automatically determined from other parameters.
* **debug.loot_path_visualization.draw_incomplete_paths**: Draw failed paths created when the mod tried to find a valid NavMesh path to the loot. The line color will be white.
* **debug.loot_path_visualization.draw_complete_paths**: Draw NavMesh paths that successfully reached the loot. The line color will be blue. 
* **debug.loot_path_visualization.outline_obstacles**: Draw boundaries around the obstacles detected between the final point in completed NavMesh paths and the loot item (determined via raytracing). If the outline color is magenta, the obstacle was ignored. If the outline color is red, the mod thinks that obstacle is making the loot inaccessible. 
* **debug.loot_path_visualization.only_outline_filtered_obstacles**: Only draw boundaries around obstacles for **debug.loot_path_visualization.outline_obstacles** if they caused the mod to consider the loot inaccessible. The outline color will be red. 
* **debug.loot_path_visualization.show_obstacle_collision_points**: Draw a spherical outline at each point where a collision with an obstacle was detected between the final point in completed NavMesh paths and the loot item (determined via raytracing). Collisions with obstacles that are ignored will not be drawn. The outline color of each point will be red.
* **debug.loot_path_visualization.collision_point_radius**: The radius (in meters) for the circles used in **debug.loot_path_visualization.show_obstacle_collision_points**.
* **debug.loot_path_visualization.show_door_obstacles**: Draw an ellipsoidal outline around doors that should block NavMesh pathing either because they're locked or something is currently interacting with them. Outlines are updated every **destroy_loot_during_raid.check_loot_accessibility.door_obstacle_update_time** seconds. The outline color will be yellow.
* **debug.loot_path_visualization.door_obstacle_min_radius**: Ensures the radii for ellipsoids drawn for **debug.loot_path_visualization.show_door_obstacles** are at least this value (in meters). Otherwise, the outlines may be hard to see. 
* **adjust_raid_times.enabled**: If the mod is allowed to make you spawn into a raid late (namely as a Scav). This is **true** by default. 
* **adjust_raid_times.scav.modification_chance**: The probability (0-1) of spawning into the raid late as a Scav.
* **adjust_raid_times.scav.max_time_remaining**: The maximum time remaining in the raid (as a fraction of the original raid time) if you spawn in late as a Scav. 
* **adjust_raid_times.scav.min_time_remaining**: The minimum time remaining in the raid (as a fraction of the original raid time) if you spawn in late as a Scav. 
* **adjust_raid_times.pmc.***: Same as above but for your PMC character. 
* **adjust_raid_times.adjust_vex_chance**: If the probability that a vehicle extraction is available can be changed if you spawn in late.
* **adjust_raid_times.adjust_bot_waves**: If "missed" bot waves should spawn in within the first minute of the raid if you spawn into the raid late. If you notice more stuttering when bots spawn into the map, you may want to disable this. 
* **adjust_raid_times.can_reduce_starting_loot**: If the initial loot on the map should be reduced if you spawn in late. This setting is ignored if **destroy_loot_during_raid.enabled=true**.

* **destroy_loot_during_raid.enabled**: If the mod is allowed to remove loot throughout the raid. If you spawn into the raid late, loot will be immediately removed from the map until it reaches the target amount for the fraction of time remaining in the raid. This is **true** by default. 
* **destroy_loot_during_raid.exclusion_radius**: The radius (in meters) from you within which loot is not allowed to be despawned. By default, this is set to 40 meters. 
* **destroy_loot_during_raid.min_loot_age**: Loot must be on the map (either loose or in a container) for at least this long (in seconds) before it's allowed to be despawned. This does not apply to loot initially generated on the map. This prevents loot on bots from being destroyed too quickly after they're killed, and it prevents items you drop from being despawned to quickly (if the mod settings allow this to happen). 
* **destroy_loot_during_raid.max_destroy_rate**: The maximum rate at which items are allowed to be despawned (in items/s). This is ignored when initially despawning loot at the beginning of Scav raids. 
* **destroy_loot_during_raid.map_traversal_speed_mps**: The rate at which a typical player traverses the map (in m/s). This should not be the maximum speed a player can run in an open area because not all players are rushing Resort, Dorms, etc. at the beginning of every raid. Increase this value to despawn loot in map hot-spots (i.e. Resort) earlier in the raid. With the default setting, you have 2-3 min to get to Resort before loot can despawn there. 
* **destroy_loot_during_raid.min_distance_traveled_for_update**: The distance you need to travel (in meters) before the mod decides if loot should be despawned (from the last time loot was despawned). This shouldn't be changed in most cases.
* **destroy_loot_during_raid.min_time_before_update_ms**: The minimum time that must elapse (in milliseconds) after loot was despawned before the mod is allowed to despawn loot again. If you're having performance issues, try increasing this. 
* **destroy_loot_during_raid.max_time_before_update_ms**: The maximum time that elapses (in milliseconds) after loot was despawned before the mod checks if loot should be despawned again. 
* **destroy_loot_during_raid.max_calc_time_per_frame_ms**: The maximum amount of time (in milliseconds) the mod is allowed to run loot-despawning procedures per frame. By default this is set to 5ms, and delays of <15ms are basically imperceptible. 
* **destroy_loot_during_raid.max_time_without_destroying_any_loot**: The maximum time (in seconds) after loot was despawned before the mod forces at least one piece of loot to despawn. This is included for compatibility with Kobrakon's Immersive Raids mod. By default, this is set to 60 seconds. 
* **destroy_loot_during_raid.ignore_items_dropped_by_player.enabled**: If items dropped by the player should not be allowed to be despawned. This allows you to effectively "hide" items and return for them later. This is **true** by default. 
* **destroy_loot_during_raid.ignore_items_dropped_by_player.only_items_brought_into_raid**: If items dropped by the player should not be allowed to be despawned only if they're not FIR. Items in your Scav character's starting inventory are not marked as FIR, just like your PMC's. This is **false** by default. 
* **destroy_loot_during_raid.ignore_items_on_dead_bots.enabled**: If the mod should not be allowed to despawn items on dead bots. This is **true** by default. 
* **destroy_loot_during_raid.ignore_items_on_dead_bots.only_if_you_killed_them**: If the mod should not be allowed to despawn items on dead bots only if you killed the bot. If you did not kill the bot, items in its inventory are still eligible for despawning. This is **true** by default. 
* **destroy_loot_during_raid.excluded_parents**: Items that are children of these parent-item ID's will not be allowed to despawn. **Entries in this array should NOT be removed, or the mod may not work properly.** 

* **destroy_loot_during_raid.check_loot_accessibility.enabled**: Check the accessibility of loot to determine if it's allowed to be despawned. If this is **false**, all loot will be considered accessible unless it's in a locked container. See the description below about how accessibility is determined. **If you're having performance issues, try disabling this.**
* **destroy_loot_during_raid.check_loot_accessibility.exclusion_radius**: All loot that is at least this distance (in meters) away from locked or inaccessible doors will automatically be assumed to be accessible. The larger this radius is, the more false negatives are likely to occur when determining loot accessibility, and the more computation time will be required. However, a setting that is too small will result in false positives if the loot is positioned within a locked room but far from the door. This is **25** by default and should work for larger locked rooms like the KIBA store in Interchange. 
* **destroy_loot_during_raid.check_loot_accessibility.max_path_search_distance**: Do not check accessibility for loot that is within the **destroy_loot_during_raid.check_loot_accessibility.exclusion_radius** of a locked door but more than this distance (in meters) from you, a bot, or a spawn point. If this value is small, the accessibility of some loot will never be checked by the mod unless you or a bot walk near it, so it will never be eligible for despawning. If this value is large, most (if not all) loot will be checked, but it may be computationally expensive. 
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_search_max_distance_player**: If the mod needs to check if loot is accessible from a location on the map (spawn point, player position, etc.), that location must be within this distance (in meters) from the NavMesh. If this value is small, the accessibility of some loot will never be checked by the mod, so it will never be eligible for despawning. If this value is large, there may be a small performance impact. This is **10** by default.
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_search_max_distance_loot**: If the mod needs to check if loot is accessible, it must be within this distance (in meters) from the NavMesh. If this value is small, the mod will not be able to calculate a NavMesh path to it, so it will never be eligible for despawning. If this value is large, the mod will be more likely to try and access the loot through floors and ceilings (thanks to a Unity quirk), and there may be a performance impact. This is **2** by default, and values much larger than this are not recommended. 
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_height_offset_complete**: When a complete NavMesh path is found to access loot, the points in the path will then be offset to be this distance (in meters) higher. This is done to make the path easier to see when **debug.loot_path_visualization.enabled=true**, and it reduces the number of obstacles that may exist between the final NavMesh point and the loot item. This is **1.25** by default, which is approximately shoulder-level. 
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_height_offset_incomplete**: When a valid NavMesh path cannot found to access loot, the points in the path will then be offset to be this distance (in meters) higher. This is **1** by default, and it's done to make the path easier to see when **debug.loot_path_visualization.enabled=true**. 
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_obstacle_min_height**: Obstacles that are less than this value in height (in meters) will be ignored when checking the accessibility of loot. Obstacles are detected via raycasting from the last NavMesh point in a complete path to the loot item. This is **0.9** by default. 
* **destroy_loot_during_raid.check_loot_accessibility.navmesh_obstacle_min_volume**: Obstacles that have an overall volume below this value (in cubic meters) will be ignored when checking the accessibility of loot. The volume is determine by the overall bounds of the object, which can be visualized via **debug.loot_path_visualization.enabled=true**, not the actual volume of the object mesh. Obstacles are detected via raycasting from the last NavMesh point in a complete path to the loot item. This is **2** by default. Values below 1 are not recommended because some loot in filing cabinets will be considered inaccessible by the mod. 
* **destroy_loot_during_raid.check_loot_accessibility.max_calc_time_per_frame_ms**: The maximum amount of time (in milliseconds) the mod is allowed to toggle NavMesh obstacles (namely for locked doors) per frame. By default this is set to **4ms**, and delays of <15ms are basically imperceptible. 
* **destroy_loot_during_raid.check_loot_accessibility.door_obstacle_update_time**: How frequently (in seconds) the mod checks if any NavMesh obstacles (namely for doors) should be toggled. This is **2** by default.

* **destroy_loot_during_raid.loot_ranking.enabled**: If loot should be ranked and destroyed in order of its calculated "value" (which is more complicated than simply cost). 
* **destroy_loot_during_raid.loot_ranking.randomness**: The amount of "randomness" (defined as a percentage of the total loot-value range in the map) to apply when destroying ranked loot. A value of 0 is like playing in a lobby full of cheaters, and loot will be despawned exactly in order of its calculated value. A value of 100 means that the worst loot in the map has a small chance of despawning first, and vice versa. A value of >>100 is like playing in a lobby full of noobs who have no idea what to pick up (in which case you might as well simply disable loot ranking). 
* **destroy_loot_during_raid.loot_ranking.alwaysRegenerate**: If the loot-ranking data is forced to generate every time the game starts. If you tend to check out new mods that may adjust item values, add new items, etc., you should make this **true** to ensure the loot-ranking data is valid for your specific SPT configuration. If you tend to install a few mods and stick with them, it should be safe to leave this at **false**. If any of the following loot-ranking parameters are changed, the loot-ranking data will be forced to regenerate.
* **destroy_loot_during_raid.loot_ranking.child_item_limits.count**: The maximum number of items and child items allowed to be despawned at one time. This is to prevent full backpacks from being despawned instantly. 
* **destroy_loot_during_raid.loot_ranking.child_item_limits.total_weight**: The maximum combined weight of an item and its child items above which they will not be allowed to despawn. This is to prevent full backpacks from being despawned instantly. If the item has no child items, this limit is ignored. 
* **destroy_loot_during_raid.loot_ranking.weighting.default_inventory_id**: The ID of the default inventory for the player, which is needed to see what items are allowed to be equipped. **This should NOT be changed, or the mod may not work properly.** 
* **destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot**: How much the calculated loot-ranking value of each item should be affected by its cost-per-slot. If the item can be directly equipped (backpacks, weapons, helmets, etc.), it's treated as occupying a single slot. Otherwise, the mod takes the price of the item (the maximum found in *handbook.json* and *prices.json*) and divides that by its size (*length* * *width*) to determine its cost-per-slot.
* **destroy_loot_during_raid.loot_ranking.weighting.weight**: How much the calculated loot-ranking value of each item should be affected by its weight.
* **destroy_loot_during_raid.loot_ranking.weighting.size**: How much the calculated loot-ranking value of each item should be affected by its size (*length* * *width*).
* **destroy_loot_during_raid.loot_ranking.weighting.gridSize**: How much the calculated loot-ranking value of each item should be affected by the number of grid slots it has (for rigs, backpacks, etc.).
* **destroy_loot_during_raid.loot_ranking.weighting.max_dim**: How much the calculated loot-ranking value of each item should be affected by its maximum dimension (either *length* or *width*).
* **destroy_loot_during_raid.loot_ranking.weighting.armor_class**: How much the calculated loot-ranking value of each item should be affected by its armor class (which is 0 if not applicable). 
* **destroy_loot_during_raid.loot_ranking.weighting.parents.xxx.name**: If **xxx** is a parent of the item, its calculated loot-ranking value has an additional value applied to it. For each entry in the **parents** dictionary, **name** simply exists for readability. You can make this whatever you want to help you remember what the ID (key) for the dictionary is.
* **destroy_loot_during_raid.loot_ranking.weighting.parents.xxx.value**: If **xxx** is a parent of the item, its calculated loot-ranking value is adjusted by this value.

* **open_doors_during_raid.enabled**: If the mod can open/close doors throughout the raid. This is **true** by default. 
* **open_doors_during_raid.can_open_locked_doors**: If the mod is allowed to open locked doors. This is **true** by default. 
* **open_doors_during_raid.can_breach_doors**: If the mod is allowed to open doors that can only be breached. This is **true** by default. 
* **open_doors_during_raid.exclusion_radius**: The radius (in meters) from you within which doors are not allowed to be opened/closed. By default, this is set to **40** meters. 
* **open_doors_during_raid.min_raid_ET**: The minimum time (in seconds) that must elapse in the raid (not necessarily from the time you spawn into the raid, namely as a Scav) before the mod is allowed to begin opening/closing doors. By default, this is set to **180** seconds.
* **open_doors_during_raid.min_raid_time_remaining**: The minimum time (in seconds) that must be remaining in the raid for the mod to be allowed to open/close doors. By default, this is **300** seconds. 
* **open_doors_during_raid.time_between_door_events**: The time (in seconds) that must elapse after the mod opens/closes doors before it's allowed to open/close doors again. By default, this is **60** seconds. 
* **open_doors_during_raid.percentage_of_doors_per_event**: The percentage of eligible doors on the map that should be opened or closed per event. By default, this is **3%**. 
* **open_doors_during_raid.chance_of_unlocking_doors**: The chance (in percent) that the mod will be able to unlock a door when trying to open it. By default, this is set to **50%**. 
* **open_doors_during_raid.chance_of_closing_doors**: The chance (in percent) that the mod will close a door instead of opening a door. By default, this is set to **15%**. 
* **open_doors_during_raid.max_calc_time_per_frame_ms**: The maximum amount of time (in milliseconds) the mod is allowed to run door-event procedures per frame. By default this is set to **3ms**, and delays of <15ms are basically imperceptible. 

* **adjust_bot_spawn_chances.enabled**: If the mod is allowed to change bot spawn-chance settings. This is **true** by default. 
* **adjust_bot_spawn_chances.adjust_bosses**: If the mod is allowed to change boss spawn chances. This is **true** by default. 
* **adjust_bot_spawn_chances.update_rate**: The time (in seconds) that must elapse after the mod updates PMC conversion-rate chances before it updates them again.  
* **adjust_bot_spawn_chances.excluded_bosses**: The names of bot types that should not be included when changing boss spawn chances. **Entries in this array should NOT be removed, or the mod may not work properly.** 

* **trader_stock_changes.enabled**: If the mod should allow trader stock to deplete as well as change the number and variety of items sold by Fence.
* **trader_stock_changes.max_ammo_buy_rate**: The maximum rate at which a trader's ammo supply (for each type) can be reduced in rounds/second.
* **trader_stock_changes.max_item_buy_rate**: The maximum rate at which a trader's item supply (for each type) can be reduced in items/second.
* **trader_stock_changes.item_sellout_chance.min**: The minimum chance (in percent) that any item in a trader's inventory can be sold out just before the trader's inventory resets.
* **trader_stock_changes.item_sellout_chance.max**: The maximum chance (in percent) that any item in a trader's inventory can be sold out just before the trader's inventory resets.
* **trader_stock_changes.barter_trade_sellout_factor**: A multiplier applied to **trader_stock_changes.item_sellout_chance** for barter items. 
* **trader_stock_changes.hot_item_sell_chance_global_multiplier**: A multiplier applied to all values in *hotITems.json*.
* **trader_stock_changes.ammo_parent_id**: The parent ID of loose ammo, which is needed to determine what items are ammo. **This should NOT be changed, or the mod may not work properly.** 
* **trader_stock_changes.money_parent_id**: The parent ID of money, which is needed to determine what items are barter trades. **This should NOT be changed, or the mod may not work properly.** 
* **trader_stock_changes.fence_stock_changes.always_regenerate**: If the list of items sold by Fence should be regenerated whenever you refresh it. This is **false** by default like in the base game.
* **trader_stock_changes.fence_stock_changes.assort_size**: The number of items sold by Fence at LL1. This is **190** by default compared to the base game's **120**.
* **trader_stock_changes.fence_stock_changes.assort_size_discount**: The number of items sold by Fence at LL2. This is **90** by default compared to the base game's **50**.
* **trader_stock_changes.fence_stock_changes.assort_restock_threshold**: If Fence's stock drops below this percentage of **trader_stock_changes.fence_stock_changes.assort_size** or **trader_stock_changes.fence_stock_changes.assort_size_discount**, his inventory will be forced to regenerate. 
* **trader_stock_changes.fence_stock_changes.maxPresetsPercent**: The maximum percentage of **trader_stock_changes.fence_stock_changes.assort_size** that can be filled with weapons. This overrides **fence.maxPresetsPercent** in the SPT-AKI *trader.json* config file.
* **trader_stock_changes.fence_stock_changes.max_preset_cost**: Any weapons that exceeds this cost after adjusting for **item_cost_fraction_vs_durability** will be removed from Fence's inventory. 
* **trader_stock_changes.fence_stock_changes.min_allowed_item_value**: Fence will always be able to sell any item below this price (using the maximum found in *handbook.json* and *prices.json*) regardless of the chance of selling it as determined by the **fence_item_value_permitted_chance** array.
* **trader_stock_changes.fence_stock_changes.max_ammo_stack**: The largest stack of any type of ammo allowed in Fence's inventory.
* **trader_stock_changes.fence_stock_changes.sell_chance_multiplier**: A multiplier applied to **trader_stock_changes.item_sellout_chance** for determining how likely an item in Fence's inventory is to be sold. 
* **trader_stock_changes.fence_stock_changes.itemTypeLimits_Override**: A dictionary describing the maximum number of items of a given type that Fence is allowed to sell per reset. If an entry for the type already exists in **fence.itemTypeLimits** in the SPT-AKI *trader.json* config file, its value will be overriden with this one. Otherwise, it will be added to that dictionary. 
* **trader_stock_changes.fence_stock_changes.blacklist_append**: The ID's that should be added to Fence's blacklist, which is initially set by **fence.blacklist** in the SPT-AKI *trader.json* config file. 
* **trader_stock_changes.fence_stock_changes.blacklist_remove**:The ID's that should be removed from Fence's blacklist, which is initially set by **fence.blacklist** in the SPT-AKI *trader.json* config file. 
* **trader_stock_changes.fence_stock_changes.blacklist_ammo_penetration_limit**: Any ammo that has a penetration value above this will be removed from Fence's inventory. 
* **trader_stock_changes.fence_stock_changes.blacklist_ammo_damage_limit**: Any ammo that has a damage value above this will be removed from Fence's inventory.

* **loot_multipliers**: [time_remaining_factor, reduction_factor] pairs describing the fraction of the accessible loot pool that should be remaining on the map based on the fraction of time remaining in the raid. A value of "1" means match the original loot amount. 
* **vex_chance_reduction**: [time_remaining_factor, reduction_factor] pairs describing how the chance that a vehicle extract is available changes based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 
* **pmc_spawn_chance_multipliers**: [time_remaining_factor, reduction_factor] pairs describing how the PMC-conversion chance should change based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 
* **boss_spawn_chance_multipliers**: [time_remaining_factor, reduction_factor] pairs describing how the boss-spawn chances should change based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 
* **fence_item_value_permitted_chance**: [item_value, sell_chance_percent] pairs describing how likely Fence is to sell an item with a certain value. 
* **item_cost_fraction_vs_durability**: [item_durability_fraction, price_multiplier] pairs describing how much cheaper Fence will sell degraded items. This applies to anything with durability (namely weapons and armor) and items with limited uses like medkits.

The mod uses the following process to determine which loot is accessible:
1. If the loot was previously determined to be accessible, it will always be considered accessible for the rest of the raid. 
2. If the loot is in a locked container, it's considered inaccessible.
3. If **destroy_loot_during_raid.check_loot_accessibility.enabled=false**, all other loot is considered accessible, and none of the other conditions below are checked. 
4. If the loot appeared on the map after the raid started, assume it's accessible. That means it was dropped by the player, is on a dead bot, or is in an airdrop.
5. If the loot is more than **destroy_loot_during_raid.check_loot_accessibility.exclusion_radius** meters from any locked/inaccessible doors, it's considered accessible.
6. If the accessibility of the loot is still unknown, the mod finds the nearest location on the map from the following:
    * Spawn points (both Scav and PMC)
    * You
    * Alive bots
7. If the loot is within **destroy_loot_during_raid.check_loot_accessibility.max_path_search_distance** meters of any of the locations above, the mod checks if the one nearest to the loot item is within **destroy_loot_during_raid.check_loot_accessibility.navmesh_search_max_distance_player** meters from the NavMesh. If either check fails, the mod assumes the loot is inaccessible. 
8. The mod checks if the loot is within **destroy_loot_during_raid.check_loot_accessibility.navmesh_search_max_distance_loot** meters from the NavMesh. If not, the mod assumes the loot is inaccessible. 
9. The mod tries finding a path via the NavMesh from the selected location in the list above to the loot. If it fails to find a complete path, the mod assumes the loot is inaccessible. 
10. If a complete path is found, the mod checks if obstacles exist between the end of the path and the loot item via raytracing. Obstacles that have a height below **destroy_loot_during_raid.check_loot_accessibility.navmesh_obstacle_min_height** meters or have an overall (bounds) volume below **destroy_loot_during_raid.check_loot_accessibility.navmesh_obstacle_min_volume** cubic meters are ignored. Foliage and bots are also ignored. If there are any remaining obstacles detected via raytracing that aren't ignored, the mod assumes the loot is inaccessible. 
11. If all checks above pass, the loot is considered accessible.

The loot-ranking system uses the following logic to determine the "value" of each item:
* Quest items and items of type "Node" are excluded from the loot-ranking data because the mod will never despawn them. 
* The cost of each item is the maximum value for it found in *handbook.json* and *prices.json*. 
* To determine the cost-per-slot of an item, its cost (determined above) is divided by its size. Items that can be directly equipped (rigs, backpacks, weapons, etc.) are treated as having a size of 1. 
* If the item is a weapon, the mod first tries finding the most desirable version of it (in terms of size and weight) available from traders. If no traders sell it, the mod will then find the most desirable preset for the weapon. If there are no presets for the weapon (as may be the case for mod-generated weapons), one with the cheapest and fewest parts possible will be generated. 
* When the mod determines the size of a weapon, it's folded if possible. 

After the loot-ranking data is generated, it's saved in *user\mods\DanW-LateToTheParty-#.#.#\db*. The ranking data can then be viewed using *user\mods\DanW-LateToTheParty-#.#.#\db\LootRankingDataReader.exe*. The program requires .NET 6.0 to run. 

If you're using this mod along with Kobrakon's Immersive Raids mod, please change the following in *config.json*:
* **adjust_raid_times.enabled** to false
* **destroy_loot_during_raid.max_time_without_destroying_any_loot** to any value you want. This is the frequency (in seconds) at which an item is removed from the map. If this value is small and you stay in the raid for a long time, you'll eventually have no more loot on the map.

Known issues:
* If **debug.enabled=true**, you cannot press the "Ready" button early when loading into a map or the script that changes the raid time (and related settings) won't run. However, if **debug.enabled=false**, the script is called twice unless you press "Ready" early. 
* Any locked door on the map is equally likely to be opened, including those locked with rare keys and those nobody ever really opens/closes in live Tarkov. 
* Some items have no price defined in *handbook.json* or *prices.json*, which makes the mod rank them as being extremely undesirable (i.e. the AXMC .338 rifle). This will hopefully be fixed as the data dumps available to the SPT developers improve. 
* If **destroy_loot_during_raid.check_loot_accessibility.enabled=false**, loot can be despawned behind locked doors or in locked containers. If **destroy_loot_during_raid.check_loot_accessibility.enabled=true**, some loot is falsely considered inaccessible and will never be despawned.
* The "hot items" sold by traders are always the same, regardless of your player level or account age. This makes the trader stock changes always seem like it's early wipe. 
* Traders may sell out of junk ammo that nobody actually buys.