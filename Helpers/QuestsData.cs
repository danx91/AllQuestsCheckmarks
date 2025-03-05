using System.Collections.Generic;
using Newtonsoft.Json.Linq;

using SPT.Common.Http;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class QuestsData
    {
        public class QuestItems
        {
            public int count = 0;
            public bool fir = false;

            public QuestItems(int count, bool fir)
            {
                this.count = count;
                this.fir = fir;
            }
        }

        public class QuestValues
        {
            public QuestItems count;
            public string name;
            public string localizedName;

            public QuestValues(int count, bool isFir, string name, string localizedName)
            {
                this.count = new QuestItems(count, isFir);
                this.name = name;
                this.localizedName = localizedName;
            }
        }

        public class ItemData
        {
            public int fir = 0;
            public int nonFir = 0;
            public int total {
                get
                {
                    return fir + nonFir;
                }
            }
            public Dictionary<string, QuestValues> quests = new Dictionary<string, QuestValues>();
        }

        public class QuestRequirements
        {
            public string questId;
            public List<QuestRequirements> list = new List<QuestRequirements>();

            public QuestRequirements(string questId)
            {
                this.questId = questId;
            }
        }

        private static readonly string COLLECTOR_ID = "5c51aac186f77432ea65c552";

        private static JArray questsData;
        private static readonly List<string> unreachableQuests = new List<string>();

        public static Dictionary<string, ItemData> questItemsByItemId = new Dictionary<string, ItemData>();
        public static Dictionary<string, Dictionary<string, QuestItems>> questItemsByQuestId = new Dictionary<string, Dictionary<string, QuestItems>>();

        public static async void LoadData()
        {
            Plugin.LogSource.LogInfo("Requesting quests data...");
            questsData = JArray.Parse(await RequestHandler.GetJsonAsync("/all-quests-checkmarks/quests"));
            CheckUnreachableQuests();
            ParseData();
        }

        private static void CheckUnreachableQuests()
        {
            unreachableQuests.Clear();

            Dictionary<string, QuestRequirements> dependencyList = new Dictionary<string, QuestRequirements>();
            List<string> allQuestIds = new List<string>();
            List<string> unreachableRootQuestIds = new List<string>();

            for (int i = 0; i < questsData.Count; ++i)
            {
                JObject quest = questsData[i] as JObject;
                string questId = quest["_id"].ToString();
                if (quest["conditions"] == null || quest["conditions"]["AvailableForStart"] == null)
                {
                    Plugin.LogSource.LogError($"Quest {questId} is missing finish conditions!");
                    continue;
                }

                allQuestIds.Add(questId);

                if (quest["isUnreachable"]?.ToString() == "True")
                {
                    unreachableRootQuestIds.Add(questId);
                }

                if (!dependencyList.TryGetValue(questId, out QuestRequirements questRequirements))
                {
                    questRequirements = new QuestRequirements(questId);
                    dependencyList.Add(questId, questRequirements);
                }

                JArray startConditions = quest["conditions"]["AvailableForStart"] as JArray;
                for (int j = 0; j < startConditions.Count; ++j)
                {
                    JObject condition = startConditions[j] as JObject;
                    string conditionType = condition["conditionType"]?.ToString();

                    if (conditionType == null)
                    {
                        Plugin.LogSource.LogError($"Quest {questId} is missing start condition #{j}!");
                        continue;
                    }
                    else if (conditionType != "Quest")
                    {
                        continue;
                    }

                    string requirementId = condition["target"].ToString();

                    if (!dependencyList.TryGetValue(requirementId, out QuestRequirements requirement))
                    {
                        requirement = new QuestRequirements(requirementId);
                        dependencyList.Add(requirementId, requirement);
                    }

                    questRequirements.list.Add(requirement);
                }
            }

            foreach(string questId in allQuestIds)
            {
                if(IsQuestUnreachable(dependencyList[questId], unreachableRootQuestIds))
                {
                    Plugin.LogDebug($"Quest {questId} is unreachable");
                    unreachableQuests.Add(questId);
                }
            }
        }

        private static bool IsQuestUnreachable(QuestRequirements requirements, List<string> unreachable, Dictionary<string, bool> cache = null)
        {
            if(cache == null)
            {
                cache = new Dictionary<string, bool>();
            }

            if (unreachable.Contains(requirements.questId))
            {
                return true;
            }
            else if (requirements.list.Count == 0)
            {
                return false;
            }
            else if (cache.TryGetValue(requirements.questId, out bool cached))
            {
                return cached;
            }

            foreach (QuestRequirements req in requirements.list)
            {
                if(IsQuestUnreachable(req, unreachable, cache))
                {
                    cache.Add(requirements.questId, true);
                    return true;
                }
            }

            cache.Add(requirements.questId, false);
            return false;
        }

        private static void ParseData()
        {
            Plugin.LogSource.LogInfo("Parsing quests data...");

            questItemsByItemId.Clear();
            questItemsByQuestId.Clear();

            for (int i = 0; i < questsData.Count; ++i)
            {
                JObject quest = questsData[i] as JObject;
                string questId = quest["_id"].ToString();

                if (unreachableQuests.Contains(questId))
                {
                    continue;
                }
                else if (quest["conditions"] == null || quest["conditions"]["AvailableForFinish"] == null)
                {
                    Plugin.LogSource.LogError($"Quest {questId} is missing finish conditions!");
                    continue;
                }
                else if (questId == COLLECTOR_ID && !Settings.includeCollector.Value)
                {
                    Plugin.LogDebug("Collector skipped");
                    continue;
                }
                else if (!Settings.includeLoyaltyRegain.Value && QuestsHelper.TRUST_REGAIN_QUESTS.Contains(questId))
                {
                    continue;    
                }

                JArray finishConditions = quest["conditions"]["AvailableForFinish"] as JArray;
                for(int j = 0; j < finishConditions.Count; ++j)
                {
                    JObject condition = finishConditions[j] as JObject;
                    string conditionType = condition["conditionType"]?.ToString();
                    bool isLeaveAtLocation = conditionType == "LeaveItemAtLocation";
                    bool fir = condition["onlyFoundInRaid"]?.ToString() == "True";

                    if (conditionType == null)
                    {
                        Plugin.LogSource.LogError($"Quest {questId} is missing finish condition #{j}!");
                        continue;
                    }
                    else if (conditionType != "HandoverItem" && conditionType != "FindItem" && !isLeaveAtLocation)
                    {
                        continue;
                    }
                    else if (condition["target"] == null)
                    {
                        Plugin.LogSource.LogError($"Quest {questId} condition #{j} is missing target!");
                        continue;
                    }
                    else if (!fir && !Settings.includeNonFir.Value)
                    {
                        Plugin.LogDebug($"Quest {questId} condition #{j} skipped (Non-FIR disabled)");
                        continue;
                    }

                    if (!int.TryParse(condition["value"].ToString(), out int count))
                    {
                        Plugin.LogSource.LogError($"Quest {questId} condition #{j} failed to parse 'value' as int!");
                        continue;
                    }

                    List<string> targets = (condition["target"] as JArray).ToObject<List<string>>();
                    foreach(string itemId in targets)
                    {
                        if(conditionType != "HandoverItem" && QuestsHelper.SPECIAL_BLACKLIST.Contains(itemId))
                        {
                            continue;
                        }
                        else if(isLeaveAtLocation && HasItemInFindOrHandover(itemId, finishConditions))
                        {
                            Plugin.LogDebug($"Quest have both handover/find and leave (questId={questId}, itemId={itemId}, fir={fir})");
                            continue;
                        }

                        AddItem(itemId, count, fir, isLeaveAtLocation, questId, quest["QuestName"]?.ToString(), quest["name"].ToString());
                    }
                }
            }
        }

        private static bool HasItemInFindOrHandover(string itemId, JArray conditions)
        {
            for (int i = 0; i < conditions.Count; ++i)
            {
                JObject condition = conditions[i] as JObject;
                string conditionType = condition["conditionType"]?.ToString();

                if (conditionType != "HandoverItem" && conditionType != "FindItem")
                {
                    continue;
                }

                if ((condition["target"] as JArray).ToObject<List<string>>().Contains(itemId))
                {
                    return true;
                }
            }

            return false;
        }
        public static void AddItem(string itemId, int count, bool fir, bool skipCheck, string questId, string questName, string questLocalizedName)
        {
            //Add to quest list
            if(!questItemsByQuestId.TryGetValue(questId, out Dictionary<string, QuestItems> questItems))
            {
                questItems = new Dictionary<string, QuestItems>();
                questItemsByQuestId.Add(questId, questItems);
            }

            if(questItems.TryGetValue(itemId, out QuestItems q) && !skipCheck)
            {
                Plugin.LogDebug($"Quest {questId} already has item {itemId} - skip");
                return;
            }
            else if(q != null)
            {
                Plugin.LogDebug($"Add duplicate [1] (questId={questId}, itemId={itemId}, savedFir={q.fir}, fir={fir})");
                q.count += count;
            }
            else
            {
                questItems.Add(itemId, new QuestItems(count, fir));
            }

            //Add to item list
            if(!questItemsByItemId.TryGetValue(itemId, out ItemData items))
            {
                items = new ItemData();
                questItemsByItemId.Add(itemId, items);
            }

            if(items.quests.TryGetValue(questId, out QuestValues questValues))
            {
                if(!skipCheck)
                {
                    Plugin.LogSource.LogError($"THIS SHOULD NEVER HAPPEN! questId={questId}, itemId={itemId}");
                    return;
                }

                Plugin.LogDebug($"Add duplicate [2] (questId={questId}, itemId={itemId}, savedFir={q.fir}, fir={fir})");
                questValues.count.count += count;
            }
            else
            {
                items.quests.Add(questId, new QuestValues(count, fir, questName ?? "Unknown Quest", questLocalizedName));
            }

            if (fir)
            {
                items.fir += count;
            }
            else
            {
                items.nonFir += count;
            }
        }

        public static void RemoveQuest(string questId)
        {
            Plugin.LogDebug($"Removing quest {questId}");

            if (!questItemsByQuestId.TryGetValue(questId, out Dictionary<string, QuestItems> questItems))
            {
                Plugin.LogSource.LogError($"Attempted to remove non-existing quest {questId} from quest data!");
                return;
            }

            foreach(KeyValuePair<string, QuestItems> item in questItems)
            {
                item.Deconstruct(out string itemId, out QuestItems items);

                if(!questItemsByItemId.TryGetValue(itemId, out ItemData itemData))
                {
                    Plugin.LogSource.LogError($"Attepted to remove non-existing quest item {item.Key} from quest {questId} data!");
                    continue;
                }

                itemData.quests.Remove(questId);

                if (items.fir)
                {
                    itemData.fir -= items.count;
                }
                else
                {
                    itemData.nonFir -= items.count;
                }

                if(itemData.quests.Count == 0)
                {
                    if(itemData.total != 0)
                    {
                        Plugin.LogSource.LogWarning($"Quest {questId} list for item {itemId} is empty, but item count is non-zero ({itemData.total})!");
                    }

                    questItemsByItemId.Remove(item.Key);
                }
            }

            questItemsByQuestId.Remove(questId);
        }
    }
}
