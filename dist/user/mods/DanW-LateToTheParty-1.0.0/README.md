Ever notice that as a Scav you get access to the same loot as your PMC along with the same raid times? Not anymore!

Just like in live Tarkov, your Scav character will no longer spawn into the map at the start of the raid. Instead, you will have an unknown amount of time remaining in it. If you think that's bad enough, there are other consequences too:
* The amount of loot remaining in the map will scale with the remaining raid time. If you spawn into a raid that's nearly over, don't expect much left for you to grab.
* The chance that car extracts will still be available also scales with the remaining raid time. The later you spawn into the raid, the less likely it is that a car will be left for you use for extraction.

To make things even more interesting, your PMC character also has a small chance of spawning into the raid late (although not by much).

Optionally, you can also have all of the "missed" bot waves spawn into the map all within the first minute of starting the raid to make the map more challenging to navigate. However, this option is disabled by default because it may require a lot of CPU power. I also left it disabled in case you'd rather use a mod like SWAG to manage bot spawning. 

This mod is highly customizable by modifying the *config.json* file. You can change:
* The odds of spawning into the raid late (as either a PMC or Scav)
* The range of time in which you'll spawn into a raid late (defined as a fraction of the original raid time)
* The reduction of loot quantity available (defined as a fraction of the original loot quantity)
* The reduction in the chance that a vehicle extract will be available (defined as a fraction of the original chance)

The arrays for **loot_multipliers** and **vex_chance_reduction** are defined using pairs of [time_remaining_factor, reduction_factor] pairs. You can have as many pairs as you'd like in the arrays; the mod will just interpolate between them. 

**If you have suggestions to modify the arrays in *config.json* to better match your experience in live Tarkov, please let me know! I only have ~100 hours of live experience, so I based my initial settings on that. I'd love to get feedback from the veteran players of live Tarkov!**

Unfortunately, while the loot quantity scales with remaing raid time, the loot quality does not. This could be good or bad depending on your perspective. Maybe I'll change this in a future release...