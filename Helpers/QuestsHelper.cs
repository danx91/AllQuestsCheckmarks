using System.Collections.Generic;
using System.Linq;
using Comfort.Common;

using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class QuestsHelper
    {
        public static readonly Color DEFAULT_COLOR = new Color(1, 1, 1, 0.706f);
        public static readonly string COLLECTOR_ID = "5c51aac186f77432ea65c552";
        public static readonly List<string> SPECIAL_BLACKLIST = new List<string>()
        {
            "5991b51486f77447b112d44f", //MS2000 Marker
            "5ac78a9b86f7741cca0bbd8d", //Signal Jammer
            "5b4391a586f7745321235ab2" //Wi-Fi Camera
        };
        public static readonly List<string> TRUST_REGAIN_QUESTS = new List<string>
        {
            //Lightkeeper
            "626148251ed3bb5bcc5bd9ed", //Make Amends - Buyout
            "6261482fa4eb80027c4f2e11", //Make Amends - Equipment
            "6391d90f4ed9512be67647df", //Make Amends
            //Fence
            "61e6e5e0f5b9633f6719ed95", //Compensation for Damage - Trust
            "61e6e60223374d168a4576a6", //Compensation for Damage - Wager
            "61e6e615eea2935bc018a2c5", //Compensation for Damage - Barkeep
            "61e6e621bfeab00251576265", //Compensation for Damage - Collection
            //Chemical - Part 4
            "59ca1a6286f774509a270942", // No Offence (Prapor)
            "59c93e8e86f7742a406989c4", // Loyalty Buyout (Skier)
            "59c9392986f7742f6923add2", // Trust Regain (Therapist)
        };

        private static readonly Dictionary<string, ItemsCount> itemsCache = new Dictionary<string, ItemsCount>();

        public enum ECheckmarkStatus
        {
            None = 0,
            Fir = 1,
            Active = 2,
            Future = 3,
            Squad = 4,
            Fulfilled = 5,
            Collector = 6
        }

        public class ItemsCount
        {
            public int fir = 0;
            public int nonFir = 0;
            public int total
            {
                get
                {
                    return fir + nonFir;
                }
            }
        }

        public class CurrentQuest
        {
            public RawQuestClass template;
            public ConditionItem condition;

            public CurrentQuest(RawQuestClass template, ConditionItem condition)
            {
                this.template = template;
                this.condition = condition;
            }
        }

        public static ItemsCount GetItemsInStash(string itemId)
        {
            ItemsCount itemsCount = new ItemsCount();

            Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
            IEnumerable<Item> items;

            if (IsInRaid())
            {
                if (itemsCache != null && itemsCache.TryGetValue(itemId, out ItemsCount cached))
                {
                    itemsCount.fir += cached.fir;
                    itemsCount.nonFir += cached.nonFir;
                }

                if (!Settings.includeRaidItems.Value)
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
                    itemsCount.fir += item.StackObjectsCount;
                }
                else
                {
                    itemsCount.nonFir += item.StackObjectsCount;
                }
            }

            return itemsCount;
        }

        public static void BuildItemsCache()
        {
            itemsCache.Clear();

            Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
            IEnumerable<Item> itemsToCache = profile.Inventory.GetPlayerItems(EPlayerItems.HideoutStashes);
            IEnumerable<Item> stashItems = Singleton<HideoutClass>.Instance?.AllStashItems;
            
            if(stashItems != null)
            {
                itemsToCache = itemsToCache.Concat(stashItems);
            }

            foreach(Item item in itemsToCache)
            {
                if(itemsCache.TryGetValue(item.TemplateId, out ItemsCount itemsCount))
                {
                    if (item.MarkedAsSpawnedInSession)
                    {
                        itemsCount.fir += item.StackObjectsCount;
                    }
                    else
                    {
                        itemsCount.nonFir += item.StackObjectsCount;
                    }
                }
                else
                {
                    ItemsCount count = new ItemsCount();

                    if (item.MarkedAsSpawnedInSession)
                    {
                        count.fir = item.StackObjectsCount;
                    }
                    else
                    {
                        count.nonFir = item.StackObjectsCount;
                    }

                    itemsCache.Add(item.TemplateId, count);
                }
            }
        }

        public static bool IsInRaid()
        {
            bool? inRaid = Singleton<AbstractGame>.Instance?.InRaid;
            return inRaid.HasValue && inRaid.Value;
        }

        public static bool IsNeededForActiveOrFutureQuests(Item item, out QuestsData.ItemData quests)
        {
            if(QuestsData.questItemsByItemId.TryGetValue(item.TemplateId, out quests) && (item.MarkedAsSpawnedInSession && quests.total > 0 || quests.nonFir > 0))
            {
                return true;
            }

            return false;
        }

        public static bool GetActiveQuestsWithItem(Profile profile, Item item, out Dictionary<string, CurrentQuest> activeQuests,
            out Dictionary<string, CurrentQuest> fulfilled)
        {
            bool activeNonFir = false;
            activeQuests = new Dictionary<string, CurrentQuest>();
            fulfilled = new Dictionary<string, CurrentQuest>();

            foreach(QuestDataClass questDataClass in profile.QuestsData)
            {
                if(questDataClass.Template == null || (questDataClass.Status != EQuestStatus.Started && questDataClass.Status != EQuestStatus.AvailableForFinish))
                {
                    continue;
                }

                foreach(KeyValuePair<EQuestStatus, GClass3779> keyValuePair in questDataClass.Template.Conditions)
                {
                    keyValuePair.Deconstruct(out EQuestStatus questStatus, out GClass3779 conditions);

                    ConditionItem tmpCondition = null;
                    bool isFulfilled = false;

                    foreach(Condition condition in conditions)
                    {
                        if (condition is ConditionItem conditionItem && conditionItem.target.Contains(item.StringTemplateId))
                        {
                            isFulfilled = questDataClass.CompletedConditions.Contains(condition.id);
                            tmpCondition = conditionItem;

                            if(conditionItem is ConditionHandoverItem)
                            {
                                break;
                            }
                        }
                    }

                    if(tmpCondition != null)
                    {
                        if (isFulfilled)
                        {
                            fulfilled.Add(questDataClass.Template.Id, new CurrentQuest(questDataClass.Template, tmpCondition));
                        }
                        else
                        {
                            activeQuests.Add(questDataClass.Template.Id, new CurrentQuest(questDataClass.Template, tmpCondition));

                            if (!tmpCondition.onlyFoundInRaid)
                            {
                                activeNonFir = true;
                            }
                        }
                        break;
                    }
                }
            }

            if(activeQuests.Count == 0)
            {
                return false;
            }
            else if (item.QuestItem)
            {
                return true;
            }

            if (item is Weapon weapon)
            {
                foreach (KeyValuePair<string, CurrentQuest> quest in new Dictionary<string, CurrentQuest>(activeQuests))
                {
                    if (quest.Value.condition is ConditionWeaponAssembly conditionWeaponAssembly)
                    {
                        if (Inventory.IsWeaponFitsCondition(weapon, conditionWeaponAssembly, false))
                        {
                            return true;
                        }
                        else
                        {
                            activeQuests.Remove(quest.Key);
                        }
                    }
                }
            }

            return activeNonFir || item.MarkedAsSpawnedInSession;
        }
        
        public static ECheckmarkStatus GetCheckmarkStatus(bool active, bool future, bool squad, bool fir, bool enough, bool collector)
        {
            if (enough && (active || future))
            {
                if(Settings.hideFulfilled.Value && IsInRaid())
                {
                    return squad ? ECheckmarkStatus.Squad : (fir ? ECheckmarkStatus.Fir : ECheckmarkStatus.None);
                }
                else if(Settings.markEnoughItems.Value)
                {
                    return squad ? ECheckmarkStatus.Squad : ECheckmarkStatus.Fulfilled;
                }
            }

            if (active)
            {
                return ECheckmarkStatus.Active;
            }
            else if (future)
            {
                return collector ? ECheckmarkStatus.Collector : ECheckmarkStatus.Future;
            }
            else if (squad)
            {
                return ECheckmarkStatus.Squad;
            }
            else if (fir)
            {
                return ECheckmarkStatus.Fir;
            }

            return ECheckmarkStatus.None;
        }

        public static void SetCheckmark(QuestItemViewPanel panel, Image image, Sprite sprite, Color color)
        {
            panel.ShowGameObject();
            image.sprite = sprite;
            image.color = color;
        }
    }
}
