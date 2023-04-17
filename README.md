Ever notice that you can find tons of loot even at the end of a raid? How about that your Scav character gets just as much time in raid as your PMC? Not anymore!

To simulate players looting throughout the raid, items will now be gradually removed from the map as the raid progresses (for both your Scav and PMC characters). This includes loot on dead AI bots! Additionally, just like in live Tarkov, your Scav character will no longer spawn into the map at the start of the raid. Instead, you will have an unknown amount of time remaining in it. To make things even more interesting, your PMC character also has a small chance of spawning into the raid late (although not by much).

Also, with the help of fellow modder DrakiaXYZ, doors randomly toggle throughout the raid, including doors that are initially locked. While this mod generally makes the game more difficult, this feature allows you to complete some quests that are normally locked behind a key and it gives you access to a slightly larger loot pool than normal. 

Optionally, you can also have all of the "missed" bot waves spawn into the map all within the first minute of starting the raid to make the map more challenging to navigate. However, this option is disabled by default because it may require a lot of CPU power. I also left it disabled in case you'd rather use a mod like SWAG to manage bot spawning. 

This mod is highly customizable by modifying the *config.json* file. You can change:
* The odds of spawning into the raid late (as either a PMC or Scav)
* The range of time in which you'll spawn into a raid late (defined as a fraction of the original raid time)
* The reduction of loot available as the raid progresses (defined as a fraction of the original loot quantity)
* The reduction in the chance that a vehicle extract will be available if you spawn into the raid late (defined as a fraction of the original chance)
* Whether loot will be gradually despawned throughout the raid or be static. **This may affect performance on slower computers**. If **destroy_loot_during_raid** is disabled, the map will still have reduced loot if you spawn in late (according to the **loot_multipliers** table); the amount just won't change after the start of the raid. 
* The **exclusion_radius** for how far away items need to be from you (in meters) for the mod to consider despawning them
* The rate at which a typical player traverses the map. This should not be the maximum speed a player can run in an open area because not all players are rushing Resort, Dorms, etc. at the beginning of every raid. Increase this value to despawn loot in map hot-spots (i.e. Resort) earlier in the raid. With the default setting, you have 2-3 min to get to Resort before loot can despawn there. 
* How often the mod decides which loot to despawn (defined by the number of meters you traveled since the last update). Starting with the 1.1.1 release, this shouldn't be changed in most cases.
* How often the mod decides which loot to despawn (defined by the minimum and maximum milliseconds since the last time the mod checked). If you're having performance issues, try increasing **min_time_before_update_ms**. 
* The maximum amount of time (in milliseconds) the mod is allowed to run procedures per frame. By default this is set to 5ms, and delays of <15ms are basically imperceptible. 
* If items you brought into the raid are eligible for despawning if you drop them. For example, if you drop your backpack during a fight and then travel beyond the **exclusion_radius** setting, it might not be there when you return for it! This option is disabled by default. Please note: items in your Scav character's starting inventory are included in this list, not just your PMC's. 
* If the mod should prevent any items that were ever placed in your inventory (either brought into the raid or FIR) from being despawned. This allows you to effectively "hide" items you picked up during the raid and return for them later. This is the default setting. 
* If the mod is allowed to open doors that are initially locked or doors that have to be breached (i.e. the door in Factory for Chemical Part 3).
* The **exclusion_radius** setting to prevent doors from opening/closing too close to you.
* The minimum elapsed time in the raid and the minimum time remaining in the raid for the mod to randomly open or close doors.
* The frequency at which the mod opens/closes doors and the percentage of doors in the map changed per event
* The chance of the mod closing doors instead of opening them

The arrays for **loot_multipliers** and **vex_chance_reduction** are defined using [time_remaining_factor, reduction_factor] pairs. You can have as many pairs as you'd like in the arrays; the mod will just interpolate between them.

For a future release, I plan on creating a ranking system for which loot to remove from the map first. Currently, the loot that's despawned is completely random. 

**If you have suggestions to modify the arrays in *config.json* to better match your experience in live Tarkov, please let me know! I only have ~100 hours of live experience, so I based my initial settings on that. I'd love to get feedback from the veteran players of live Tarkov!**

Known issues:
* If you spawn into the map late, some stuttering may occur at the very beginning of the raid because the mod needs to despawn a lot of items at once. 
* Some stuttering may occur after AI bots are killed because the mod suddenly finds a lot of new loot to manage. 
* The mod tends to despawn containers (i.e. backpacks) along with all of their contents at the same time instead of gradually removing their contents first. This isn't totally unrealistic, but the mod is a bit aggressive with it. 
* Airdrop loot will never despawn unless you pick it up and drop it elsewhere while **only_items_brought_into_raid=true**
* If **debug=true**, you cannot press the "Ready" button early when loading into a map or the script that changes the raid time (and related settings) won't run. However, if **debug=false**, the script is called twice unless you press "Ready" early. 
* The mod is equally likely to open any door on the map, including those locked with rare keys. 