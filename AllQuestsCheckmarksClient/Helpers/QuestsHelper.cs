using System.Collections.Generic;
using System.Linq;
using Comfort.Common;

using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI.DragAndDrop;
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

        public class CurrentQuest
        {
            public RawQuestClass Template;
            public ConditionItem Condition;

            public CurrentQuest(RawQuestClass template, ConditionItem condition)
            {
                Template = template;
                Condition = condition;
            }
        }

        public static bool IsInRaid()
        {
            bool? inRaid = Singleton<AbstractGame>.Instance?.InRaid;
            return inRaid.HasValue && inRaid.Value;
        }

        public static bool IsNeededForActiveOrFutureQuests(Item item, out QuestsData.ItemData quests)
        {
            return QuestsData.QuestItemsByItemId.TryGetValue(item.TemplateId, out quests) && 
                   (item.MarkedAsSpawnedInSession && quests.Total > 0 || quests.NonFir > 0);
        }

        public static bool GetActiveQuestsWithItem(Profile profile, Item item, out Dictionary<string, CurrentQuest> activeQuests,
            out Dictionary<string, CurrentQuest> fulfilled)
        {
            bool activeNonFir = false;
            activeQuests = new Dictionary<string, CurrentQuest>();
            fulfilled = new Dictionary<string, CurrentQuest>();

            foreach(QuestDataClass questDataClass in profile.QuestsData)
            {
                if(questDataClass.Template == null || 
                   (questDataClass.Status != EQuestStatus.Started && questDataClass.Status != EQuestStatus.AvailableForFinish))
                {
                    continue;
                }

                foreach(KeyValuePair<EQuestStatus, GClass1631> keyValuePair in questDataClass.Template.Conditions)
                {
                    keyValuePair.Deconstruct(out _, out GClass1631 conditions);

                    ConditionItem? tmpCondition = null;
                    bool isFulfilled = false;

                    foreach(Condition condition in conditions)
                    {
                        if (!(condition is ConditionItem conditionItem) || !conditionItem.target.Contains(item.StringTemplateId))
                        {
                            continue;
                        }
                        
                        isFulfilled = questDataClass.CompletedConditions.Contains(condition.id);
                        tmpCondition = conditionItem;

                        if(conditionItem is ConditionHandoverItem)
                        {
                            break;
                        }
                    }

                    if (tmpCondition == null)
                    {
                        continue;
                    }
                    
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

            if(activeQuests.Count == 0)
            {
                return false;
            }

            if (item.QuestItem)
            {
                return true;
            }

            if (!(item is Weapon weapon))
            {
                return activeNonFir || item.MarkedAsSpawnedInSession;
            }
            
            foreach (KeyValuePair<string, CurrentQuest> quest in new Dictionary<string, CurrentQuest>(activeQuests))
            {
                if (!(quest.Value.Condition is ConditionWeaponAssembly conditionWeaponAssembly))
                {
                    continue;
                }
                
                if (Inventory.IsWeaponFitsCondition(weapon, conditionWeaponAssembly))
                {
                    return true;
                }
                
                activeQuests.Remove(quest.Key);
            }

            return activeNonFir || item.MarkedAsSpawnedInSession;
        }
        
        public static ECheckmarkStatus GetCheckmarkStatus(bool active, bool future, bool squad, bool fir, bool enough, bool collector)
        {
            if (enough && (active || future))
            {
                if(Settings.HideFulfilled!.Value && IsInRaid())
                {
                    return squad ? ECheckmarkStatus.Squad : fir ? ECheckmarkStatus.Fir : ECheckmarkStatus.None;
                }

                if (Settings.MarkEnoughItems!.Value)
                {
                    return squad ? ECheckmarkStatus.Squad : ECheckmarkStatus.Fulfilled;
                }
            }

            if (active)
            {
                return ECheckmarkStatus.Active;
            }

            if (future)
            {
                return collector ? ECheckmarkStatus.Collector : ECheckmarkStatus.Future;
            }

            if (squad)
            {
                return ECheckmarkStatus.Squad;
            }

            if (fir)
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
