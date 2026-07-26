using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace LateToTheParty.Models
{
    public class ItemCollectionWrapper
    {
        public Item ParentItem { get; private set; }
        public IEnumerable<Item> ChildItems { get; private set; }

        public MongoId ParentTemplateId => ParentItem.Template;
        public int ChildItemCount => ChildItems.Count();

        public ItemCollectionWrapper(Item parentItem, IEnumerable<Item> childItems)
        {
            ParentItem = parentItem;
            ChildItems = childItems;
        }

        public ItemCollectionWrapper(Preset preset)
        {
            ParentItem = preset.Items[0];
            ChildItems = preset.Items[1..];
        }
    }
}
