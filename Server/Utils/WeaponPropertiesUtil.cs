using LateToTheParty.Configuration;
using LateToTheParty.Helpers;
using LateToTheParty.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using System.Net.Mail;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class WeaponPropertiesUtil
    {
        private LoggingUtil _loggingUtil;
        private DatabaseService _databaseService;
        private ItemInfoUtil _itemInfoUtil;

        public WeaponPropertiesUtil(LoggingUtil loggingUtil, DatabaseService databaseService, ItemInfoUtil itemInfoUtil)
        {
            _loggingUtil = loggingUtil;
            _databaseService = databaseService;
            _itemInfoUtil = itemInfoUtil;
        }

        public ItemPropertiesConfig GetWeaponProperties(TemplateItem baseItem, IEnumerable<Item> attachments)
        {
            int width = baseItem.Properties?.Width ?? 0;
            int height = baseItem.Properties?.Height ?? 0;
            double weight = baseItem.Properties?.Weight ?? 0;

            foreach (Item attachment in attachments)
            {
                if (attachment.Template == baseItem.Id)
                {
                    continue;
                }

                if (!_databaseService.GetTemplates().Items.TryGetValue(attachment.Template, out TemplateItem? attachmentTemplate) || (attachmentTemplate == null))
                {
                    continue;
                }

                weight += attachmentTemplate.Properties?.Weight ?? 0;

                if ((attachment.SlotId != null) && (baseItem.Properties?.Foldable == true) && (baseItem.Properties?.FoldedSlot == attachment.SlotId))
                {
                    continue;
                }

                width += attachmentTemplate.Properties?.ExtraSizeLeft ?? 0;
                width += attachmentTemplate.Properties?.ExtraSizeRight ?? 0;
                height += attachmentTemplate.Properties?.ExtraSizeUp ?? 0;
                height += attachmentTemplate.Properties?.ExtraSizeDown ?? 0;
            }

            return new ItemPropertiesConfig(width, height, weight);
        }

        public ItemPropertiesConfig GetWeaponProperties(ItemCollectionWrapper weapon)
        {
            if (_databaseService.GetTemplates().Items.TryGetValue(weapon.ParentTemplateId, out TemplateItem? baseItemTemplate) && (baseItemTemplate != null))
            {
                return GetWeaponProperties(baseItemTemplate, weapon.ChildItems);
            }

            throw new InvalidOperationException($"Cannot find template for base weapon item {weapon.ParentTemplateId}");
        }

        public ItemPropertiesConfig GetWeaponProperties(Preset preset)
        {
            return GetWeaponProperties(new ItemCollectionWrapper(preset));
        }

        public IEnumerable<Preset> FindMatchingPresets(TemplateItem item)
        {
            return _databaseService.GetGlobals().ItemPresets.Values.Where(preset => preset.Items[0].Template == item.Id);
        }

        public IEnumerable<ItemCollectionWrapper> FindMatchesInTraderAssorts(TemplateItem item)
        {
            IEnumerable<Trader> traders = _databaseService.GetTraders().Values
                .NotIncludingFence()
                .WithOffers();

            foreach (Trader trader in traders)
            {
                foreach (Item assortItem in trader.Assort.AssortOfferBaseItems())
                {
                    if (assortItem.Template != item.Id)
                    {
                        continue;
                    }

                    IEnumerable<Item> childItems = trader.Assort.GetAllChildItems(assortItem);
                    ItemCollectionWrapper weapon = new ItemCollectionWrapper(assortItem, childItems);

                    if (IsFullyAssembled(weapon))
                    {
                        yield return weapon;
                    }
                }
            }
        }

        public bool IsFullyAssembled(ItemCollectionWrapper weapon)
        {
            TemplateItem? parentTemplate = _itemInfoUtil.GetTemplate(weapon.ParentTemplateId);
            if (parentTemplate == null)
            {
                _loggingUtil.Error($"Could not get template for parent item {weapon.ParentItem.Id}");
                return false;
            }

            if (parentTemplate.Properties?.Slots == null)
            {
                return true;
            }

            string weaponName = _itemInfoUtil.GetLocalizedName(parentTemplate);

            foreach (Slot slot in parentTemplate.Properties.Slots)
            {
                if (slot.Required != true)
                {
                    continue;
                }

                IEnumerable<Item> childItemsInSlot = weapon.ChildItems.Where(item => item.SlotId == slot.Id);
                if (!childItemsInSlot.Any())
                {
                    _loggingUtil.Info($"Ignoring incomplete weapon build for {weaponName} with missing attachment in {slot.Name ?? "[NULL SLOT]"}");
                    return false;
                }
            }

            return true;
        }
    }
}
