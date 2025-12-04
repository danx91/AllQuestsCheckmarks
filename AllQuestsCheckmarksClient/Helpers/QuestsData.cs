using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;
using System.Linq;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class QuestsData
    {
        public class QuestItems(int count, bool fir)
        {
            public int Count = count;
            public bool Fir = fir;
        }

        public class QuestValues(int count, bool isFir, string name, string localizedName)
        {
            public QuestItems Count = new(count, isFir);
            public string Name = name;
            public string LocalizedName = localizedName;
        }

        public class ItemData
        {
            public int Fir;
            public int NonFir;
            public int Total => Fir + NonFir;
            public Dictionary<MongoID, QuestValues> Quests = [];
        }

        public class QuestRequirements(MongoID questId)
        {
            public MongoID QuestId = questId;
            public List<QuestRequirements> List = [];
        }

        private static List<QuestJson>? _questsData;
        private static readonly List<MongoID> _unreachableQuests = [];

        public static Dictionary<MongoID, ItemData> QuestItemsByItemId = [];
        public static Dictionary<MongoID, Dictionary<MongoID, QuestItems>> QuestItemsByQuestId = [];

        public static async void LoadData()
        {
            Plugin.LogSource?.LogInfo("Requesting quests data...");

            string response = await RequestHandler.GetJsonAsync("/all-quests-checkmarks/quests");
            _questsData = JsonConvert.DeserializeObject<List<QuestJson>>(response, JsonSettingsProvider.Settings);

            Plugin.LogDebug(response);

            if (_questsData == null)
            {
                Plugin.LogSource?.LogError("Failed to parse _questData!");
                Plugin.LogSource?.LogError(response);
                return;
            }

            CheckUnreachableQuests();
            ParseData();
        }

        private static void CheckUnreachableQuests()
        {
            _unreachableQuests.Clear();

            if(Settings.IncludeUnreachable!.Value)
            {
                Plugin.LogDebug("Including unreachable quests - skip check");
                return;
            }

            Dictionary<MongoID, QuestRequirements> dependencyList = [];
            List<MongoID> allQuestIds = [];
            List<MongoID> unreachableRootQuestIds = [];

            for (int i = 0; i < _questsData!.Count; ++i)
            {
                QuestJson quest2 = _questsData[i];
                if (quest2.Quest is not Quest quest)
                {
                    Plugin.LogSource?.LogError("Quest is null!");
                    continue;
                }

                if (quest.Id is not MongoID questId)
                {
                    Plugin.LogSource?.LogError("Quest.Id is null!");
                    continue;
                }

                if (quest.Conditions?.AvailableForStart is not List<AvailableForStartCondition> startConditions)
                {
                    Plugin.LogSource?.LogError($"Quest {questId} is missing start conditions!");
                    continue;
                }

                allQuestIds.Add(questId);

                if (quest2.IsUnreachable is true)
                {
                    unreachableRootQuestIds.Add(questId);
                }

                if (!dependencyList.TryGetValue(questId, out QuestRequirements questRequirements))
                {
                    questRequirements = new QuestRequirements(questId);
                    dependencyList.Add(questId, questRequirements);
                }
                
                for (int j = 0; j < startConditions.Count; ++j)
                {
                    AvailableForStartCondition condition = startConditions[j];
                    
                    if (condition.ConditionType is not string conditionType)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} is missing start condition #{j} conditionType!");
                        continue;
                    }

                    if (conditionType != "Quest")
                    {
                        continue;
                    }
                    
                    if (condition.Target is not string requirementIdRaw)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} is missing start condition #{j} target!");
                        continue;
                    }

                    if (!AQCUtils.IsValidMongoID(requirementIdRaw))
                    {
                        Plugin.LogDebug($"Quest {questId} start condition #{j} has non-MongoID target: ${requirementIdRaw}!");
                        continue;
                    }

                    MongoID requirementId = requirementIdRaw;

                    if (!dependencyList.TryGetValue(requirementId, out QuestRequirements requirement))
                    {
                        requirement = new QuestRequirements(requirementId);
                        dependencyList.Add(requirementId, requirement);
                    }

                    questRequirements.List.Add(requirement);
                }
            }

            Dictionary<MongoID, bool> cache = [];

            foreach (MongoID questId in allQuestIds)
            {
                if (IsQuestUnreachable(dependencyList[questId], unreachableRootQuestIds, cache))
                {
                    Plugin.LogDebug($"Quest {questId} is unreachable");
                    _unreachableQuests.Add(questId);
                }
            }
        }

        private static bool IsQuestUnreachable(QuestRequirements requirements, List<MongoID> unreachableRoot, Dictionary<MongoID, bool> cache, Stack<MongoID>? stack = null)
        {
            stack ??= new();

            //Check for circular dependency - assume reachable
            if (stack.Contains(requirements.QuestId))
            {
                Plugin.LogDebug($"Circular dependency detected! Quest {requirements.QuestId} is already in stack!");
                return false;
            }

            //Root is unreachable - unreachable
            if (unreachableRoot.Contains(requirements.QuestId))
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} has unreachable root quest!");
                return true;
            }

            //Empty requirements - reachable
            if (requirements.List.Count == 0)
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} has no requirements!");
                return false;
            }

            //Already checked - return cached value
            if (cache.TryGetValue(requirements.QuestId, out bool cached))
            {
                Plugin.LogDebug($"Quest {requirements.QuestId} already checked - returning cached value ({cached})");
                return cached;
            }

            //Push current quest to stack before traversing list
            stack.Push(requirements.QuestId);

            //Traverse quest requirements list
            foreach (QuestRequirements req in requirements.List)
            {
                if (IsQuestUnreachable(req, unreachableRoot, cache, stack))
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
            Plugin.LogSource?.LogInfo("Parsing quests data...");

            QuestItemsByItemId.Clear();
            QuestItemsByQuestId.Clear();

            for (int i = 0; i < _questsData!.Count; ++i)
            {
                QuestJson quest2 = _questsData[i];
                if (quest2.Quest is not Quest quest)
                {
                    Plugin.LogSource?.LogError("Quest is null!");
                    continue;
                }

                if(quest.Id is not MongoID questId)
                {
                    Plugin.LogSource?.LogError("Quest[_id] is null!");
                    continue;
                }

                if (_unreachableQuests.Contains(questId))
                {
                    continue;
                }

                if (quest.Conditions?.AvailableForFinish is not List<AvailableForFinishCondition> finishConditions)
                {
                    Plugin.LogSource?.LogError($"Quest {questId} is missing finish conditions!");
                    continue;
                }

                if (questId == QuestsHelper.COLLECTOR_ID && !Settings.IncludeCollector!.Value)
                {
                    Plugin.LogDebug("Collector skipped");
                    continue;
                }

                if (!Settings.IncludeLoyaltyRegain!.Value &&  QuestsHelper.TRUST_REGAIN_QUESTS.Contains(questId))
                {
                    continue;    
                }
                
                for(int j = 0; j < finishConditions.Count; ++j)
                {
                    AvailableForFinishCondition condition = finishConditions[j];
                    if (condition.ConditionType is not string conditionType)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} is missing finish condition #{j} or its conditionType!");
                        continue;
                    }

                    bool isLeaveAtLocation = conditionType == "LeaveItemAtLocation";
                    bool fir = condition.OnlyFoundInRaid is true;

                    if (conditionType != "HandoverItem" && conditionType != "FindItem" && !isLeaveAtLocation)
                    {
                        continue;
                    }

                    if (!fir && !Settings.IncludeNonFir!.Value)
                    {
                        Plugin.LogDebug($"Quest {questId} condition #{j} skipped (Non-FIR disabled)");
                        continue;
                    }

                    if (condition.Target is not List<string> targetsRaw)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} condition #{j} is missing targets!");
                        continue;
                    }

                    List<MongoID> targets = [.. targetsRaw.Where(AQCUtils.IsValidMongoID).Select(t => new MongoID(t))];

                    if (condition.Value is not int count)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} condition #{j} is missing count");
                        continue;
                    }

                    foreach(MongoID itemId in targets)
                    {
                        if(conditionType != "HandoverItem" && QuestsHelper.SPECIAL_BLACKLIST.Contains(itemId))
                        {
                            continue;
                        }

                        if(isLeaveAtLocation && HasItemInFindOrHandover(itemId, finishConditions))
                        {
                            Plugin.LogDebug($"Quest have both handover/find and leave (questId={questId}, itemId={itemId}, fir={fir})");
                            continue;
                        }

                        AddItem(itemId, count, fir, isLeaveAtLocation, questId, quest.QuestName, quest.LocalizedName);
                    }
                }
            }
        }

        private static bool HasItemInFindOrHandover(MongoID itemId, List<AvailableForFinishCondition> conditions)
        {
            for (int i = 0; i < conditions.Count; ++i)
            {
                AvailableForFinishCondition condition = conditions[i];
                if (condition.ConditionType is not string conditionType)
                {
                    Plugin.LogSource?.LogError("Condition is null or missing conditionType!");
                    continue;
                }

                if (conditionType != "HandoverItem" && conditionType != "FindItem")
                {
                    continue;
                }

                if (condition.Target is not List<string> targetsRaw)
                {
                    Plugin.LogSource?.LogError("Condition is missing targets!");
                    continue;
                }

                List<MongoID> targets = [.. targetsRaw.Where(AQCUtils.IsValidMongoID).Select(t => new MongoID(t))];

                if (targets.Contains(itemId))
                {
                    return true;
                }
            }

            return false;
        }
        public static void AddItem(MongoID itemId, int count, bool fir, bool skipCheck, MongoID questId, string? questName, string? questLocalizedName)
        {
            //Add to quest list
            if(!QuestItemsByQuestId.TryGetValue(questId, out Dictionary<MongoID, QuestItems> questItems))
            {
                questItems = [];
                QuestItemsByQuestId.Add(questId, questItems);
            }

            if(questItems.TryGetValue(itemId, out QuestItems q) && !skipCheck)
            {
                Plugin.LogDebug($"Quest {questId} already has item {itemId} - skip");
                return;
            }

            if(q != null)
            {
                Plugin.LogDebug($"Add duplicate [1] (questId={questId}, itemId={itemId}, savedFir={q.Fir}, fir={fir})");
                q.Count += count;
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
                    Plugin.LogSource?.LogError($"THIS SHOULD NEVER HAPPEN! questId={questId}, itemId={itemId}");
                    return;
                }

                Plugin.LogDebug($"Add duplicate [2] (questId={questId}, itemId={itemId}, savedFir={q?.Fir}, fir={fir})");
                questValues.Count.Count += count;
            }
            else
            {
                items.Quests.Add(questId, new QuestValues(count, fir, questName ?? "Unknown Quest", questLocalizedName ?? "Unknown Quest"));
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

        public static void RemoveQuest(MongoID questId)
        {
            Plugin.LogDebug($"Removing quest {questId}");

            if (!QuestItemsByQuestId.TryGetValue(questId, out Dictionary<MongoID, QuestItems> questItems))
            {
                Plugin.LogSource?.LogError($"Attempted to remove non-existing quest {questId} from quest data!");
                return;
            }

            foreach(KeyValuePair<MongoID, QuestItems> item in questItems)
            {
                item.Deconstruct(out MongoID itemId, out QuestItems items);

                if(!QuestItemsByItemId.TryGetValue(itemId, out ItemData itemData))
                {
                    Plugin.LogSource?.LogError($"Attepted to remove non-existing quest item {item.Key} from quest {questId} data!");
                    continue;
                }

                itemData.Quests.Remove(questId);

                if (items.Fir)
                {
                    itemData.Fir -= items.Count;
                }
                else
                {
                    itemData.NonFir -= items.Count;
                }

                if (itemData.Quests.Count != 0)
                {
                    continue;
                }
                
                if(itemData.Total != 0)
                {
                    Plugin.LogSource?.LogWarning($"Quest {questId} list for item {itemId} is empty, but item count is non-zero ({itemData.Total})!");
                }

                QuestItemsByItemId.Remove(item.Key);
            }

            QuestItemsByQuestId.Remove(questId);
        }
    }
}
