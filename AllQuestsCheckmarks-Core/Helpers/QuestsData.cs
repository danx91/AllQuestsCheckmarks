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
            public QuestItems Count;
            public string Name;
            public string LocalizedName;

            public QuestValues(int count, bool isFir, string name, string localizedName)
            {
                this.Count = new QuestItems(count, isFir);
                this.Name = name;
                this.LocalizedName = localizedName;
            }
        }

        public class ItemData
        {
            public int Fir = 0;
            public int NonFir = 0;
            public int Total {
                get
                {
                    return Fir + NonFir;
                }
            }
            public Dictionary<string, QuestValues> Quests = new Dictionary<string, QuestValues>();
        }

        public class QuestRequirements
        {
            public string QuestId;
            public List<QuestRequirements> List = new List<QuestRequirements>();

            public QuestRequirements(string questId)
            {
                this.QuestId = questId;
            }
        }

        private static JArray _questsData;
        private static readonly List<string> _unreachableQuests = new List<string>();

        public static Dictionary<string, ItemData> QuestItemsByItemId = new Dictionary<string, ItemData>();
        public static Dictionary<string, Dictionary<string, QuestItems>> QuestItemsByQuestId = new Dictionary<string, Dictionary<string, QuestItems>>();

        public static async void LoadData()
        {
            Plugin.LogSource.LogInfo("Requesting quests data...");
            _questsData = JArray.Parse(await RequestHandler.GetJsonAsync("/all-quests-checkmarks/quests"));
            CheckUnreachableQuests();
            ParseData();
        }

        private static void CheckUnreachableQuests()
        {
            _unreachableQuests.Clear();

            Dictionary<string, QuestRequirements> dependencyList = new Dictionary<string, QuestRequirements>();
            List<string> allQuestIds = new List<string>();
            List<string> unreachableRootQuestIds = new List<string>();

            for (int i = 0; i < _questsData.Count; ++i)
            {
                JObject quest = _questsData[i] as JObject;
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

                    questRequirements.List.Add(requirement);
                }
            }

            Dictionary<string, bool> cache = new Dictionary<string, bool>();

            foreach (string questId in allQuestIds)
            {
                if(IsQuestUnreachable(dependencyList[questId], unreachableRootQuestIds, cache))
                {
                    Plugin.LogDebug($"Quest {questId} is unreachable");
                    _unreachableQuests.Add(questId);
                }
            }
        }

        private static bool IsQuestUnreachable(QuestRequirements requirements, List<string> unreachableRoot, Dictionary<string, bool> cache, Stack<string> stack = null)
        {
            if(stack == null)
            {
                stack = new Stack<string>();
            }

            //Check for circular dependency - assume reachable
            if (stack.Contains(requirements.QuestId))
            {
                Plugin.LogDebug($"Circular dependency detected! Quest {requirements.QuestId} is already in stack!");
                return false;
            }
            //Root is unreachable - unreachable
            else if (unreachableRoot.Contains(requirements.QuestId))
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} has unreachable root quest!");
                return true;
            }
            //Empty requirements - reachable
            else if (requirements.List.Count == 0)
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} has no requirements!");
                return false;
            }
            //Already checked - return cached value
            else if (cache.TryGetValue(requirements.QuestId, out bool cached))
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} already checked - returning cached value ({cached})");
                return cached;
            }

            //Push current quest to stack before traversing list
            stack.Push(requirements.QuestId);

            //Traverse quest requirements list
            foreach (QuestRequirements req in requirements.List)
            {
                if(IsQuestUnreachable(req, unreachableRoot, cache, stack))
                {
                    cache.Add(requirements.QuestId, true);
                    return true;
                }
            }

            //Pop current quest from stack
            stack.Pop();

            cache.Add(requirements.QuestId, false);
            return false;
        }

        private static void ParseData()
        {
            Plugin.LogSource.LogInfo("Parsing quests data...");

            QuestItemsByItemId.Clear();
            QuestItemsByQuestId.Clear();

            for (int i = 0; i < _questsData.Count; ++i)
            {
                JObject quest = _questsData[i] as JObject;
                string questId = quest["_id"].ToString();

                if (_unreachableQuests.Contains(questId))
                {
                    continue;
                }
                else if (quest["conditions"] == null || quest["conditions"]["AvailableForFinish"] == null)
                {
                    Plugin.LogSource.LogError($"Quest {questId} is missing finish conditions!");
                    continue;
                }
                else if (questId == QuestsHelper.COLLECTOR_ID && !Settings.IncludeCollector.Value)
                {
                    Plugin.LogDebug("Collector skipped");
                    continue;
                }
                else if (!Settings.IncludeLoyaltyRegain.Value && QuestsHelper.TRUST_REGAIN_QUESTS.Contains(questId))
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
                    else if (!fir && !Settings.IncludeNonFir.Value)
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
            if(!QuestItemsByQuestId.TryGetValue(questId, out Dictionary<string, QuestItems> questItems))
            {
                questItems = new Dictionary<string, QuestItems>();
                QuestItemsByQuestId.Add(questId, questItems);
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
            if(!QuestItemsByItemId.TryGetValue(itemId, out ItemData items))
            {
                items = new ItemData();
                QuestItemsByItemId.Add(itemId, items);
            }

            if(items.Quests.TryGetValue(questId, out QuestValues questValues))
            {
                if(!skipCheck)
                {
                    Plugin.LogSource.LogError($"THIS SHOULD NEVER HAPPEN! questId={questId}, itemId={itemId}");
                    return;
                }

                Plugin.LogDebug($"Add duplicate [2] (questId={questId}, itemId={itemId}, savedFir={q.fir}, fir={fir})");
                questValues.Count.count += count;
            }
            else
            {
                items.Quests.Add(questId, new QuestValues(count, fir, questName ?? "Unknown Quest", questLocalizedName));
            }

            if (fir)
            {
                items.Fir += count;
            }
            else
            {
                items.NonFir += count;
            }
        }

        public static void RemoveQuest(string questId)
        {
            Plugin.LogDebug($"Removing quest {questId}");

            if (!QuestItemsByQuestId.TryGetValue(questId, out Dictionary<string, QuestItems> questItems))
            {
                Plugin.LogSource.LogError($"Attempted to remove non-existing quest {questId} from quest data!");
                return;
            }

            foreach(KeyValuePair<string, QuestItems> item in questItems)
            {
                item.Deconstruct(out string itemId, out QuestItems items);

                if(!QuestItemsByItemId.TryGetValue(itemId, out ItemData itemData))
                {
                    Plugin.LogSource.LogError($"Attepted to remove non-existing quest item {item.Key} from quest {questId} data!");
                    continue;
                }

                itemData.Quests.Remove(questId);

                if (items.fir)
                {
                    itemData.Fir -= items.count;
                }
                else
                {
                    itemData.NonFir -= items.count;
                }

                if(itemData.Quests.Count == 0)
                {
                    if(itemData.Total != 0)
                    {
                        Plugin.LogSource.LogWarning($"Quest {questId} list for item {itemId} is empty, but item count is non-zero ({itemData.Total})!");
                    }

                    QuestItemsByItemId.Remove(item.Key);
                }
            }

            QuestItemsByQuestId.Remove(questId);
        }
    }
}
