using System.Linq;
using System.Collections.Generic;

using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
using Comfort.Common;

namespace AllQuestsCheckmarks.Helpers
{
    class StashHelper
    {
        public static readonly List<string> MoneyIds = new List<string>()
        {
            "5449016a4bdc2d6f028b456f", //Roubles
            "5696686a4bdc2da3298b456a", //Dollars
            "569668774bdc2da2298b4568", //Euros
        };

        private static readonly Dictionary<string, ItemsCount> _itemsCache = new Dictionary<string, ItemsCount>();

        public class ItemsCount
        {
            public int Fir = 0;
            public int NonFir = 0;
            public int Total
            {
                get
                {
                    return Fir + NonFir;
                }
            }
        }

        public static ItemsCount GetItemsInStash(string itemId)
        {
            ItemsCount itemsCount = new ItemsCount();

            Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
            IEnumerable<Item> items;

            if (QuestsHelper.IsInRaid())
            {
                if (_itemsCache != null && _itemsCache.TryGetValue(itemId, out ItemsCount cached))
                {
                    itemsCount.Fir += cached.Fir;
                    itemsCount.NonFir += cached.NonFir;
                }

                if (!Settings.IncludeRaidItems.Value)
                {
                    return itemsCount;
                }

                items = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.QuestItems).Where(i => i.TemplateId == itemId);
            }
            else
            {
                items = profile.Inventory.GetPlayerItems(EPlayerItems.All).Where(i => i.TemplateId == itemId);
            }

            foreach (Item item in items)
            {
                if (item.MarkedAsSpawnedInSession)
                {
                    itemsCount.Fir += item.StackObjectsCount;
                }
                else
                {
                    itemsCount.NonFir += item.StackObjectsCount;
                }
            }

            return itemsCount;
        }

        public static void BuildItemsCache()
        {
            _itemsCache.Clear();

            Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
            IEnumerable<Item> itemsToCache = profile.Inventory.GetPlayerItems(EPlayerItems.HideoutStashes);
            IEnumerable<Item> stashItems = Singleton<HideoutClass>.Instance?.AllStashItems;

            if (stashItems != null)
            {
                itemsToCache = itemsToCache.Concat(stashItems);
            }

            foreach (Item item in itemsToCache)
            {
                if (_itemsCache.TryGetValue(item.TemplateId, out ItemsCount itemsCount))
                {
                    if (item.MarkedAsSpawnedInSession)
                    {
                        itemsCount.Fir += item.StackObjectsCount;
                    }
                    else
                    {
                        itemsCount.NonFir += item.StackObjectsCount;
                    }
                }
                else
                {
                    ItemsCount count = new ItemsCount();

                    if (item.MarkedAsSpawnedInSession)
                    {
                        count.Fir = item.StackObjectsCount;
                    }
                    else
                    {
                        count.NonFir = item.StackObjectsCount;
                    }

                    _itemsCache.Add(item.TemplateId, count);
                }
            }

            Plugin.LogSource.LogInfo($"Items cache built. Total items in cache: {_itemsCache.Count}");
        }
    }
}
