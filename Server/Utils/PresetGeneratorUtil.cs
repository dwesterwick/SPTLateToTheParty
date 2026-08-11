using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Collections.Generic;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class PresetGeneratorUtil
    {
        private LoggingUtil _loggingUtil;
        private ItemInfoUtil _itemInfoUtil;

        public PresetGeneratorUtil(LoggingUtil loggingUtil, ItemInfoUtil itemInfoUtil)
        {
            _loggingUtil = loggingUtil;
            _itemInfoUtil = itemInfoUtil;
        }

        public Preset GenerateBestPreset(TemplateItem baseItemTemplate)
        {
            Item baseItem = _itemInfoUtil.CreateFromTemplate(baseItemTemplate);
            IEnumerable<Item> attachments = GetBestAttachments(baseItem, Enumerable.Empty<MongoId>());

            string baseItemName = _itemInfoUtil.GetLocalizedName(baseItemTemplate);
            Preset bestWeaponPreset = new Preset()
            {
                Id = new MongoId(),
                ChangeWeaponName = false,
                Name = baseItemName + "_AutoGenByLateToTheParty",
                Parent = baseItem.Id,
                Items = new List<Item> { baseItem }
            };
            bestWeaponPreset.Items.AddRange(attachments);

            _loggingUtil.Debug($"Created preset for {baseItemName}");
            foreach (Item item in attachments)
            {
                TemplateItem? template = _itemInfoUtil.GetTemplate(item);
                string attachmentName = _itemInfoUtil.GetLocalizedName(template!);
                _loggingUtil.Debug($"Created preset for {baseItemName}: Added {attachmentName}");
            }

            return bestWeaponPreset;
        }

        public List<Item> GetBestAttachments(Item baseItem, IEnumerable<MongoId> initialIncompatibleAttachmentIds)
        {
            if (baseItem == null)
            {
                throw new ArgumentNullException(nameof(baseItem));
            }

            List<Item> attachments = new List<Item>();
            List<MongoId> incompatibleAttachmentIds = initialIncompatibleAttachmentIds.ToList();

            TemplateItem? baseItemTemplate = _itemInfoUtil.GetTemplate(baseItem);
            if (baseItemTemplate?.Properties?.Slots == null)
            {
                _loggingUtil.Error($"Cannot get attachments for invalid base item {baseItem.Id}");
                return attachments;
            }

            string baseItemName = _itemInfoUtil.GetLocalizedName(baseItemTemplate);

            while (true)
            {
                attachments.Clear();

                Item? bestAttachment = null;
                foreach (Slot slot in baseItemTemplate.Properties.Slots)
                {
                    bestAttachment = GetBestAttachment(slot, incompatibleAttachmentIds);
                    if (bestAttachment == null)
                    {
                        continue;
                    }

                    bestAttachment.ParentId = baseItem.Id;
                    attachments.Add(bestAttachment);
                }

                if ((bestAttachment == null) || (attachments.Count == 0))
                {
                    break;
                }

                Item? firstConflictingItem = GetFirstConflictingItem(attachments);
                if (firstConflictingItem == null)
                {
                    break;
                }

                TemplateItem firstConflictingItemTemplate = _itemInfoUtil.GetTemplate(firstConflictingItem.Template)!;
                incompatibleAttachmentIds.Add(firstConflictingItemTemplate.Id);
            }

            return attachments;
        }

        public Item? GetBestAttachment(Slot slot, IEnumerable<MongoId> incompatibleAttachmentIds)
        {
            if (slot.Properties?.Filters == null)
            {
                return null;
            }

            IEnumerable<MongoId> validAttachmentTemplateIds = slot.Properties.Filters
                .SelectMany(filter => filter.Filter ?? [])
                .Where(id => !incompatibleAttachmentIds.Contains(id));

            IEnumerable<TemplateItem> validAttachmentTemplates = validAttachmentTemplateIds
                .Select(_itemInfoUtil.GetTemplate)
                .Where(template => template != null)
                .Select(template => template!);

            if (!validAttachmentTemplates.Any())
            {
                _loggingUtil.Warning($"Could not find a valid attachment to put in slot {slot.Name ?? slot.Id ?? "[NULL]"}");
                return null;
            }

            TemplateItem bestAttachmentTemplate = validAttachmentTemplates
                .OrderBy(_itemInfoUtil.GetMaxPrice)
                .Last();

            Item bestAttachment = _itemInfoUtil.CreateFromTemplate(bestAttachmentTemplate);
            bestAttachment.SlotId = slot.Id;

            return bestAttachment;
        }

        public Item? GetFirstConflictingItem(IEnumerable<Item> items)
        {
            foreach (Item item in items)
            {
                TemplateItem template = _itemInfoUtil.GetTemplate(item.Template)!;
                if (template?.Properties?.ConflictingItems == null)
                {
                    continue;
                }

                IEnumerable<Item> conflictingItems = items.Where(item => template.Properties.ConflictingItems.Contains(item.Template));
                if (!conflictingItems.Any())
                {
                    continue;
                }

                Item firstConflictingItem = conflictingItems.First();
                TemplateItem? firstConflictingItemTemplate = _itemInfoUtil.GetTemplate(firstConflictingItem);
                if (firstConflictingItemTemplate == null)
                {
                    _loggingUtil.Error($"Could not get template for item {firstConflictingItem.Id}");
                    continue;
                }

                string itemName = _itemInfoUtil.GetLocalizedName(template);
                string firstConflictingItemName = _itemInfoUtil.GetLocalizedName(firstConflictingItemTemplate);

                _loggingUtil.Info($"Cannot install {firstConflictingItemName} when {itemName} is also installed");

                return firstConflictingItem;
            }

            return null;
        }
    }
}
