Make your SPT experience closer to live Tarkov in all aspects except bot behavior and waiting for servers. Changes loot, open doors, bot spawn rates, and time remaining in the raid when your Scav spawns in. 

This mod makes the the following changes to your SPT experience:
* Loot (including on dead bots) will be gradually removed throughout the raid to simulate other players taking it. 
* Doors will randomly open and close throughout the raid to simulate players moving through the map (thanks to help from DrakiaXYZ!). If you're lucky, locked doors may be opened for you...
* Compared to vanilla SPT, PMC's are more likely to spawn toward the beginning of the raid and less likely to spawn toward the end of it. 
* Your Scav will spawn into the raid with an unknown amount of time remaining in it. However, that also means you may be able to run directly to extract and get a "Survived" status if enough time has passed in the raid. There is also a small chance that your PMC will spawn into the raid slightly late. 
* If you spawn into the map late as a Scav, bosses are less likely to spawn. 
* If you spawn into the map late as a Scav, vehicle extracts are less likely to be available. 


This mod is highly customizable by modifying the *config.json* file. Here are the settings you can change:

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
* **destroy_loot_during_raid.loot_ranking.weighting.parents.xxx.weighting**: If **xxx** is a parent of the item, its calculated loot-ranking value is adjusted by this value.

* **open_doors_during_raid.enabled**: If the mod can open/close doors throughout the raid. This is **true** by default. 
* **open_doors_during_raid.can_open_locked_doors**: If the mod is allowed to open locked doors. This is **true** by default. 
* **open_doors_during_raid.can_breach_doors**: If the mod is allowed to open doors that can only be breached. This is **true** by default. 
* **open_doors_during_raid.exclusion_radius**: The radius (in meters) from you within which doors are not allowed to be opened/closed. By default, this is set to 40 meters. 
* **open_doors_during_raid.min_raid_ET**: The minimum time (in seconds) that must elapse in the raid (not necessarily from the time you spawn into the raid, namely as a Scav) before the mod is allowed to begin opening/closing doors. By default, this is set to 180 seconds.
* **open_doors_during_raid.min_raid_time_remaining**: The minimum time (in seconds) that must be remaining in the raid for the mod to be allowed to open/close doors. By default, this is 300 seconds. 
* **open_doors_during_raid.time_between_door_events**: The time (in seconds) that must elapse after the mod opens/closes doors before it's allowed to open/close doors again. By default, this is 60 seconds. 
* **open_doors_during_raid.percentage_of_doors_per_event**: The percentage of eligible doors on the map that should be opened or closed per event. By default, this is 3%. 
* **open_doors_during_raid.chance_of_closing_doors**: The chance (in percent) that the mod will close a door instead of opening a door. By default, this is set to 15%. 
* **open_doors_during_raid.max_calc_time_per_frame_ms**: The maximum amount of time (in milliseconds) the mod is allowed to run door-event procedures per frame. By default this is set to 3ms, and delays of <15ms are basically imperceptible. 

* **adjust_bot_spawn_chances.enabled**: If the mod is allowed to change bot spawn-chance settings. This is **true** by default. 
* **adjust_bot_spawn_chances.adjust_bosses**: If the mod is allowed to change boss spawn chances. This is **true** by default. 
* **adjust_bot_spawn_chances.update_rate**: The time (in seconds) that must elapse after the mod updates PMC conversion-rate chances before it updates them again.  
* **adjust_bot_spawn_chances.excluded_bosses**: The names of bot types that should not be included when changing boss spawn chances. **Entries in this array should NOT be removed, or the mod may not work properly.** 

* **loot_multipliers**: [time_remaining_factor, reduction_factor] pairs describing the fraction of the initial loot pool that should be remaining on the map based on the fraction of time remaining in the raid. A value of "1" means match the original loot amount. 
* **vex_chance_reduction**: [time_remaining_factor, reduction_factor] pairs describing how the chance that a vehicle extract is available changes based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 
* **pmc_spawn_chance_multipliers**: [time_remaining_factor, reduction_factor] pairs describing how the PMC-conversion chance should change based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 
* **boss_spawn_chance_multipliers**: [time_remaining_factor, reduction_factor] pairs describing how the boss-spawn chances should change based on the fraction of time remaining in the raid. A value of "1" means match the original setting. 

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
* If **debug=true**, you cannot press the "Ready" button early when loading into a map or the script that changes the raid time (and related settings) won't run. However, if **debug=false**, the script is called twice unless you press "Ready" early. 
* Any door on the map is equally likely to be opened, including those locked with rare keys and those nobody ever really opens/closes in live Tarkov. 
* Some items have no price defined in *handbook.json* or *prices.json*, which makes the mod rank them as being extremely undesirable (i.e. the AXMC .338 rifle). This will hopefully be fixed as the data dumps available to the SPT developers improve. 
* Loot can be despawned behind locked doors