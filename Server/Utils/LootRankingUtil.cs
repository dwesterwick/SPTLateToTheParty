using LateToTheParty.Configuration;
using LateToTheParty.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LootRankingUtil
    {
        private LoggingUtil _loggingUtil;
        private ConfigUtil _configUtil;
        private DatabaseService _databaseService;
        private ItemInfoUtil _itemInfoUtil;
        private WeaponSearchUtil _weaponSearchUtil;

        public LootRankingUtil
        (
            LoggingUtil loggingUtil,
            ConfigUtil configUtil,
            DatabaseService databaseService,
            ItemInfoUtil itemInfoUtil,
            WeaponSearchUtil weaponSearchUtil
        )
        {
            _loggingUtil = loggingUtil;
            _configUtil = configUtil;
            _databaseService = databaseService;
            _itemInfoUtil = itemInfoUtil;
            _weaponSearchUtil = weaponSearchUtil;
        }

        public void GenerateLootRankingData()
        {
            if (!_configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Enabled)
            {
                _loggingUtil.Info("Loot ranking is disabled in config.json");
                return;
            }

            if (CanUseExistingLootRankingData())
            {
                _loggingUtil.Info("Using existing loot ranking data");
                return;
            }

            UpdateLootRankingData();
        }

        public Dictionary<string, LootRankingDataConfig> GetLootRankingData()
        {
            Dictionary<string, LootRankingDataConfig> lootRankingData = _configUtil.LootRankingData;
            return lootRankingData;
        }

        private bool CanUseExistingLootRankingData()
        {
            if (_configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.AlwaysRegenerate)
            {
                return false;
            }

            if (_configUtil.LootRankingData.Count == 0)
            {
                return false;
            }

            return true;
        }

        private void UpdateLootRankingData()
        {
            _loggingUtil.Info("Creating loot ranking data...");

            Dictionary<string, LootRankingDataConfig> newLootRankingData = new Dictionary<string, LootRankingDataConfig>();
            foreach (TemplateItem item in _databaseService.GetItems().Values)
            {
                if (item.Type == "Node")
                {
                    continue;
                }

                if (item.IsQuestItem())
                {
                    continue;
                }

                LootRankingDataConfig rankingData = GetLootRankingValue(item);
                newLootRankingData.Add(item.Id, rankingData);
            }

            _configUtil.LootRankingData = newLootRankingData;

            _loggingUtil.Info("Creating loot ranking data...done.");
        }

        private LootRankingDataConfig GetLootRankingValue(TemplateItem item)
        {
            double cost = _itemInfoUtil.GetMaxPrice(item);
            double weight = item.Properties?.Weight ?? 0;
            int size = (item.Properties?.Width * item.Properties?.Height) ?? 0;
            int maxDim = Math.Max(item.Properties?.Width ?? 0, item.Properties?.Height ?? 0);
            int gridSize = _itemInfoUtil.GetInternalGridArea(item);
            int armorClass = item.Properties?.ArmorClass ?? 0;

            if (item.Properties?.WeapClass != null)
            {
                _weaponSearchUtil.FindBestWeaponMatchFromTraders(item);
            }

            double costPerSlot = cost;
            if (!_itemInfoUtil.CanEquip(item))
            {
                costPerSlot /= size;
            }

            double parentWeighting = GetAdditionalParentWeighting(item);

            LootRankingWeightingConfig lootRankingWeightingConfig = _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting;
            double value = 0;
            value += costPerSlot * lootRankingWeightingConfig.CostPerSlot;
            value += weight * lootRankingWeightingConfig.Weight;
            value += size * lootRankingWeightingConfig.Size;
            value += gridSize * lootRankingWeightingConfig.GridSize;
            value += maxDim * lootRankingWeightingConfig.MaxDim;
            value += armorClass * lootRankingWeightingConfig.ArmorClass;
            value += parentWeighting;

            LootRankingDataConfig lootRanking = new LootRankingDataConfig()
            {
                ID = item.Id,
                Name = _itemInfoUtil.GetLocalizedName(item),
                Value = value,
                CostPerSlot = costPerSlot,
                Weight = weight,
                Size = size,
                GridSize = gridSize,
                MaxDim = maxDim,
                ArmorClass = armorClass,
                ParentWeighting = parentWeighting
            };

            return lootRanking;
        }

        private double GetAdditionalParentWeighting(TemplateItem item)
        {
            double totalParentWeighting = 0;

            foreach (NameValueConfig nameValueConfig in _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting.Parents)
            {
                if (_itemInfoUtil.IsOfBaseClass(item, nameValueConfig.Name))
                {
                    totalParentWeighting += nameValueConfig.Value;
                }
            }

            return totalParentWeighting;
        }
    }
}
