using System.Collections.Generic;
using Newtonsoft.Json.Linq;

using EFT.InventoryLogic;
using SPT.Common.Http;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class SquadQuests
    {
        public static Dictionary<string, Dictionary<string, bool>> SquadQuestsDict = new Dictionary<string, Dictionary<string, bool>>();
        private static Dictionary<string, string> _squadNicks = new Dictionary<string, string>();
        private static Dictionary<string, JArray>? _squadData;

        public static void LoadData(Dictionary<string, string> squadMembers)
        {
            _squadNicks = new Dictionary<string, string>(squadMembers);

            string response = RequestHandler.PostJson("/all-quests-checkmarks/active-quests", new JArray(_squadNicks.Keys).ToJson());
            _squadData = JObject.Parse(response)?.ToObject<Dictionary<string, JArray>>();


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

            foreach(KeyValuePair<string, JArray> keyValuePair in _squadData!)
            {
                keyValuePair.Deconstruct(out string profileId, out JArray questsData);

                Dictionary<string, bool> playerQuests = new Dictionary<string, bool>();
                SquadQuestsDict.Add(profileId, playerQuests);

                for (int i = 0; i < questsData.Count; ++i)
                {
                    if(!(questsData[i] is JObject quest) || !(quest["_id"]?.ToString() is string questId))
                    {
                        Plugin.LogSource?.LogError($"Failed to parse quest for player {profileId}!");
                        continue;
                    }

                    if (!(quest["conditions"]?["AvailableForFinish"] is JArray finishConditions))
                    {
                        Plugin.LogSource?.LogError($"Quest {questId} is missing finish conditions!");
                        continue;
                    }

                    for (int j = 0; j < finishConditions.Count; ++j)
                    {

                        if (!(finishConditions[j] is JObject condition) || !(condition["conditionType"]?.ToString() is string conditionType))
                        {
                            Plugin.LogSource?.LogError($"Quest {questId} is missing finish condition #{j} or its conditionType!");
                            continue;
                        }

                        bool fir = condition["onlyFoundInRaid"]?.ToString() == "True";

                        if (conditionType != "HandoverItem" && conditionType != "FindItem" && conditionType != "LeaveItemAtLocation")
                        {
                            continue;
                        }

                        if (!(condition["target"]?.ToObject<List<string>>() is List<string> targets))
                        {
                            Plugin.LogSource?.LogError($"Quest {questId} condition #{j} is missing targets!");
                            continue;
                        }

                        foreach (string itemId in targets)
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
            members = new List<string>();

            foreach(KeyValuePair<string, Dictionary<string, bool>> keyValuePair in SquadQuestsDict)
            {
                keyValuePair.Deconstruct(out string profileId, out Dictionary<string, bool> items);

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
