using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class TraderStockChangesConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("always_regenerate")]
        public bool AlwaysRegenerate { get; set; } = false;

        [JsonProperty("max_ammo_buy_rate")]
        public double MaxAmmoBuyRate { get; set; } = 10;

        [JsonProperty("max_item_buy_rate")]
        public double MaxItemBuyRate { get; set; } = 100;

        [JsonProperty("item_sellout_chance")]
        public double ItemSelloutChance { get; set; } = 50;

        [JsonProperty("ammo_parent_id")]
        public string AmmoParentId { get; set; } = "5485a8684bdc2da71d8b4567";

        [JsonProperty("fence_stock_changes")]
        public FenceStockChangesConfig FenceStockChanges { get; set; } = new FenceStockChangesConfig();

        [JsonProperty("hot_item_sell_chance_global_multiplier")]
        public double HotItemSellChanceGlobalMultiplier { get; set; } = 0.5;

        [JsonProperty("hot_item_sell_chance_multipliers")]
        public Dictionary<string, double> HotItemSellChanceMultipliers { get; set; } = new Dictionary<string, double>();

        public TraderStockChangesConfig()
        {

        }
    }
}
