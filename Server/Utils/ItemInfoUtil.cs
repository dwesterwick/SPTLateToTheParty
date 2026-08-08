using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using System.Collections;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ItemInfoUtil
    {
        private LoggingUtil _loggingUtil;
        private ConfigUtil _configUtil;
        private DatabaseService _databaseService;
        private ItemHelper _itemHelper;
        private LocalizationUtil _localizationUtil;

        public ItemInfoUtil
        (
            LoggingUtil loggingUtil,
            ConfigUtil configUtil,
            DatabaseService databaseService,
            ItemHelper itemHelper,
            LocalizationUtil localizationUtil)
        {
            _loggingUtil = loggingUtil;
            _configUtil = configUtil;
            _databaseService = databaseService;
            _itemHelper = itemHelper;
            _localizationUtil = localizationUtil;
        }

        private MongoId? _defaultInventoryId;
        public MongoId DefaultInventoryId
        {
            get
            {
                if (_defaultInventoryId == null)
                {
                    _defaultInventoryId = new MongoId(_configUtil.CurrentConfig.DestroyLootDuringRaid.LootRanking.Weighting.DefaultInventoryId);
                }

                return _defaultInventoryId.Value;
            }
        }

        private TemplateItem? _defaultInventory;
        public TemplateItem DefaultInventory
        {
            get
            {
                if (_defaultInventory == null)
                {
                    _defaultInventory = GetDefaultInventory();
                }

                return _defaultInventory;
            }
        }

        private TemplateItem GetDefaultInventory()
        {
            if (!_databaseService.GetTemplates().Items.TryGetValue(DefaultInventoryId, out TemplateItem? inventory))
            {
                throw new InvalidOperationException("Could not retrieve the default inventory template");
            }

            return inventory;
        }

        private Dictionary<MongoId, double?>? _handbookPrices;
        public Dictionary<MongoId, double?> HandbookPrices
        {
            get
            {
                if (_handbookPrices == null)
                {
                    _handbookPrices = GetHandbookPrices();
                }

                return _handbookPrices;
            }
        }

        private Dictionary<MongoId, double?> GetHandbookPrices()
        {
            Dictionary<MongoId, double?> handbookPrices = new Dictionary<MongoId, double?>();

            foreach (HandbookItem item in _databaseService.GetTemplates().Handbook.Items)
            {
                handbookPrices.Add(item.Id, item.Price);
            }

            return handbookPrices;
        }

        public bool IsATemplateGroup(TemplateItem item) => string.Equals(item.Type, "Node", StringComparison.OrdinalIgnoreCase);

        public string GetLocalizedName(TemplateItem item) => _localizationUtil.GetLocalizedName(item);

        public int GetInternalGridArea(TemplateItem item)
        {
            if (item.Properties?.Grids == null)
            {
                return 0;
            }

            return item.Properties.Grids.Sum(grid => (grid.Properties?.CellsH ?? 0) * (grid.Properties?.CellsV ?? 0));
        }

        public bool IsOfBaseClass(TemplateItem item, MongoId baseId) => _itemHelper.IsOfBaseclass(item.Id, baseId);
        public bool IsOfBaseClass(TemplateItem item, TemplateItem baseItem) => IsOfBaseClass(item, baseItem.Id);
        
        public bool CanEquip(TemplateItem item)
        {
            if (DefaultInventory.Properties?.Slots == null)
            {
                throw new InvalidOperationException("No slots found in the default inventory");
            }

            foreach (Slot slot in DefaultInventory.Properties.Slots)
            {
                if (slot.Properties?.Filters == null)
                {
                    continue;
                }

                foreach (SlotFilter slotFilter in slot.Properties.Filters)
                {
                    IEnumerable<MongoId> filters = slotFilter.Filter ?? Enumerable.Empty<MongoId>();
                    if (_itemHelper.IsOfBaseclasses(item.Id, filters))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public double GetMaxPrice(TemplateItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            double handbookPrice = GetHandbookPrice(item);
            double fleaPrice = GetFleaMarketPrice(item);

            return Math.Max(handbookPrice, fleaPrice);
        }

        public double GetHandbookPrice(TemplateItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!HandbookPrices.TryGetValue(item.Id, out double? handbookPrice) || (handbookPrice == null) || double.IsNaN(handbookPrice.Value))
            {
                //_loggingUtil.Warning($"Invalid handbook price for {GetLocalizedName(item)} ({item.Id}). Defaulting to 0.");
                handbookPrice = 0;
            }

            return handbookPrice.Value;
        }

        public double GetFleaMarketPrice(TemplateItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_databaseService.GetTemplates().Prices.TryGetValue(item.Id, out double fleaPrice) || double.IsNaN(fleaPrice))
            {
                //_loggingUtil.Warning($"Invalid flea market price for {GetLocalizedName(item)} ({item.Id}). Defaulting to 0.");
                fleaPrice = 0;
            }
            
            return fleaPrice;
        }

        public TemplateItem? GetTemplate(Item item) => GetTemplate(item.Template);

        public TemplateItem? GetTemplate(MongoId id)
        {
            if (_databaseService.GetTemplates().Items.TryGetValue(id, out TemplateItem? itemTemplate) && (itemTemplate != null))
            {
                return itemTemplate;
            }

            return null;
        }

        public Item CreateFromTemplate(TemplateItem template)
        {
            MongoId newItemId = new MongoId();
            Item newItem = new Item()
            {
                Id = newItemId,
                Template = template.Id
            };

            return newItem;
        }
    }
}
