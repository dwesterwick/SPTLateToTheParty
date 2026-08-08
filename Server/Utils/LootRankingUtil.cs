using LateToTheParty.Configuration;
using LateToTheParty.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using System.Diagnostics;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LootRankingUtil
    {
        private LoggingUtil _loggingUtil;
        private ConfigUtil _configUtil;
        private DatabaseService _databaseService;
        private ItemInfoUtil _itemInfoUtil;
        private WeaponPropertiesUtil _weaponPropertiesUtil;
        private PresetGeneratorUtil _presetGeneratorUtil;

        public LootRankingUtil
        (
            LoggingUtil loggingUtil,
            ConfigUtil configUtil,
            DatabaseService databaseService,
            ItemInfoUtil itemInfoUtil,
            WeaponPropertiesUtil weaponPropertiesUtil,
            PresetGeneratorUtil presetGeneratorUtil
        )
        {
            _loggingUtil = loggingUtil;
            _configUtil = configUtil;
            _databaseService = databaseService;
            _itemInfoUtil = itemInfoUtil;
            _weaponPropertiesUtil = weaponPropertiesUtil;
            _presetGeneratorUtil = presetGeneratorUtil;
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

            foreach ((MongoId id, TemplateItem item) in _databaseService.GetItems())
            {
                if (!ShouldHaveLootRankingValue(item))
                {
                    continue;
                }

                if (!_configUtil.LootRankingData.ContainsKey(id))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldHaveLootRankingValue(TemplateItem item)
        {
            if (_itemInfoUtil.IsATemplateGroup(item))
            {
                return false;
            }

            if (item.IsQuestItem())
            {
                return false;
            }

            return true;
        }

        private static readonly object _lockObject = new object();
        private void UpdateLootRankingData()
        {
            _loggingUtil.Info("Creating loot ranking data... (this might take a while)");

            Stopwatch sw = Stopwatch.StartNew();

            Dictionary<string, LootRankingDataConfig> newLootRankingData = new Dictionary<string, LootRankingDataConfig>();

            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 };
            Parallel.ForEach(_databaseService.GetItems().Values, parallelOptions, item =>
            {
                if (!ShouldHaveLootRankingValue(item))
                {
                    return;
                }

                LootRankingDataConfig rankingData = GetLootRankingValue(item);

                lock (_lockObject)
                {
                    newLootRankingData.Add(item.Id, rankingData);
                }
            });

            _configUtil.LootRankingData = newLootRankingData;

            _loggingUtil.Info($"Creating loot ranking data...done ({sw.ElapsedMilliseconds}ms).");
        }

        private LootRankingDataConfig GetLootRankingValue(TemplateItem item)
        {
            string name = _itemInfoUtil.GetLocalizedName(item);
            double cost = _itemInfoUtil.GetMaxPrice(item);
            int width = item.Properties?.Width ?? 0;
            int height = item.Properties?.Height ?? 0;
            double weight = item.Properties?.Weight ?? 0;
            int gridSize = _itemInfoUtil.GetInternalGridArea(item);
            int armorClass = item.Properties?.ArmorClass ?? 0;

            LootRankingDataConfig lootRanking = new LootRankingDataConfig()
            {
                ID = item.Id,
                Name = name,
                Width = width,
                Height = height,
                Weight = weight,
                GridSize = gridSize,
                ArmorClass = armorClass
            };

            if (item.Properties?.WeapClass != null)
            {
                ItemCollectionWrapper bestWeapon = FindBestWeapon(item);
                ItemPropertiesConfig bestWeaponProperties = _weaponPropertiesUtil.GetWeaponProperties(bestWeapon);
                lootRanking.Width = bestWeaponProperties.Width;
                lootRanking.Height = bestWeaponProperties.Height;
                lootRanking.Weight = bestWeaponProperties.Weight;
            }

            lootRanking.CostPerSlot = cost;
            if (!_itemInfoUtil.CanEquip(item))
            {
                lootRanking.CostPerSlot /= lootRanking.Size;
            }

            lootRanking.ParentWeighting = GetAdditionalParentWeighting(item);

            LootRankingWeightingConfig lootRankingWeightingConfig = _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting;
            lootRanking.Value = 0;
            lootRanking.Value += lootRanking.CostPerSlot * lootRankingWeightingConfig.CostPerSlot;
            lootRanking.Value += weight * lootRankingWeightingConfig.Weight;
            lootRanking.Value += lootRanking.Size * lootRankingWeightingConfig.Size;
            lootRanking.Value += gridSize * lootRankingWeightingConfig.GridSize;
            lootRanking.Value += lootRanking.MaxDim * lootRankingWeightingConfig.MaxDim;
            lootRanking.Value += armorClass * lootRankingWeightingConfig.ArmorClass;
            lootRanking.Value += lootRanking.ParentWeighting;

            return lootRanking;
        }

        private double GetAdditionalParentWeighting(TemplateItem item)
        {
            double totalParentWeighting = 0;

            foreach ((string parentId, NameValueConfig nameValueConfig) in _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting.Parents)
            {
                if (_itemInfoUtil.IsOfBaseClass(item, parentId))
                {
                    totalParentWeighting += nameValueConfig.Value;
                }
            }

            return totalParentWeighting;
        }

        private ItemCollectionWrapper FindBestWeapon(TemplateItem item)
        {
            IEnumerable<ItemCollectionWrapper> matchingAssortWeapons = _weaponPropertiesUtil.FindMatchesInTraderAssorts(item);
            ItemCollectionWrapper? bestWeapon = matchingAssortWeapons.OrderBy(GetWeaponValue).FirstOrDefault();
            double? bestWeaponValue = bestWeapon == null ? null : GetWeaponValue(bestWeapon);

            IEnumerable<Preset> matchingPresets = _weaponPropertiesUtil.FindMatchingPresets(item);
            Preset? bestPreset = matchingPresets.OrderBy(GetWeaponValue).FirstOrDefault();
            if (bestPreset != null)
            {
                double bestPresetValue = GetWeaponValue(bestPreset);
                if ((bestWeaponValue == null) || (bestPresetValue > bestWeaponValue))
                {
                    bestWeapon = new ItemCollectionWrapper(bestPreset);
                    bestWeaponValue = bestPresetValue;
                }
            }

            if (bestWeapon == null)
            {
                Preset newPreset = _presetGeneratorUtil.GenerateBestPreset(item);
                bestWeapon = new ItemCollectionWrapper(newPreset);
            }

            return bestWeapon;
        }

        private double GetWeaponValue(Preset preset)
        {
            ItemPropertiesConfig weaponProperties = _weaponPropertiesUtil.GetWeaponProperties(preset);
            return GetWeaponValue(weaponProperties);
        }

        private double GetWeaponValue(ItemCollectionWrapper weapon)
        {
            ItemPropertiesConfig weaponProperties = _weaponPropertiesUtil.GetWeaponProperties(weapon);
            return GetWeaponValue(weaponProperties);
        }

        private double GetWeaponValue(ItemPropertiesConfig weaponProperties)
        {
            double value = 0;
            value += weaponProperties.Size * _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting.Size;
            value += weaponProperties.Weight * _configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting.Weight;

            return value;
        }
    }
}
