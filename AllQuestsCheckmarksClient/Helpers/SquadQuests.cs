using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System.Collections.Generic;
using System.Linq;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class SquadQuests
    {
        public static Dictionary<MongoID, Dictionary<MongoID, bool>> SquadQuestsDict = [];
        private static Dictionary<MongoID, string> _squadNicks = [];
        private static Dictionary<MongoID, List<Quest>>? _squadData;

        public static void LoadData(Dictionary<string, string> squadMembers)
        {
            _squadNicks = [];

            foreach (var (key, value) in squadMembers)
            {
                if (!AQCUtils.IsValidMongoID(key))
                {
                    Plugin.LogSource?.LogError($"Invalid MongoID for coop player: {key}");
                    continue;
                }

                _squadNicks.Add(new MongoID(key), value);
            }

            string response = RequestHandler.PostJson("/all-quests-checkmarks/active-quests", new JArray(squadMembers.Keys).ToJson());
            _squadData = JsonConvert.DeserializeObject<Dictionary<MongoID, List<Quest>>>(response, JsonSettingsProvider.Settings);

            Plugin.LogDebug(response);

            if (_squadData == null)
            {
                Plugin.LogSource?.LogError("Failed to parse _squadData!");
                Plugin.LogSource?.LogError(response);
                return;
            }

            ParseData();
        }

        public static void ParseData()
        {
            SquadQuestsDict.Clear();

            foreach(KeyValuePair<MongoID, List<Quest>> keyValuePair in _squadData!)
            {
                keyValuePair.Deconstruct(out MongoID profileId, out List<Quest> questsData);

                Dictionary<MongoID, bool> playerQuests = [];
                SquadQuestsDict.Add(profileId, playerQuests);

                for (int i = 0; i < questsData.Count; ++i)
                {
                    Quest quest = questsData[i];
                    if (quest.Id is not MongoID questId)
                    {
                        Plugin.LogSource?.LogError($"Failed to parse quest for player {profileId}!");
                        continue;
                    }

                    if (quest.Conditions?.AvailableForFinish is not List<AvailableForFinishCondition> finishConditions)
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} is missing finish conditions!");
                        continue;
                    }

                    for (int j = 0; j < finishConditions.Count; ++j)
                    {
                        AvailableForFinishCondition condition = finishConditions[j];
                        if (condition.ConditionType is not string conditionType)
                        {
                            Plugin.LogSource?.LogError($"Quest {questId} condition #{j} is missing conditionType!");
                            continue;
                        }

                        bool fir = condition.OnlyFoundInRaid is true;

                        if (conditionType != "HandoverItem" && conditionType != "FindItem" && conditionType != "LeaveItemAtLocation")
                        {
                            continue;
                        }

                        if (condition.Target is not List<string> targetsRaw)
                        {
                            Plugin.LogSource?.LogError($"Quest {questId} condition #{j} is missing targets!");
                            continue;
                        }

                        List<MongoID> targets = [.. targetsRaw.Where(AQCUtils.IsValidMongoID).Select(t => new MongoID(t))];

                        foreach (MongoID itemId in targets)
                        {
                            if (conditionType != "HandoverItem" && QuestsHelper.SPECIAL_BLACKLIST.Contains(itemId))
                            {
                                continue;
                            }

                            if (!playerQuests.TryGetValue(itemId, out bool wasFir) || (!wasFir && fir))
                            {
                                playerQuests[itemId] = fir;
                            }
                        }
                    }
                }
            }
        }

        public static void ClearSquadQuests()
        {
            Plugin.LogDebug("ClearSquadQuests");
            SquadQuestsDict.Clear();
        }

        public static bool IsNeededForSquadMembers(Item item, out List<string> members)
        {
            members = [];

            foreach(KeyValuePair<MongoID, Dictionary<MongoID, bool>> keyValuePair in SquadQuestsDict)
            {
                keyValuePair.Deconstruct(out MongoID profileId, out Dictionary<MongoID, bool> items);

                if (items.TryGetValue(item.TemplateId, out bool fir) &&
                    (!fir || item.MarkedAsSpawnedInSession) &&
                    _squadNicks.TryGetValue(profileId, out string nick))
                {
                    members.Add(nick);
                }
            }

            return members.Count > 0;
        }
    }
}
