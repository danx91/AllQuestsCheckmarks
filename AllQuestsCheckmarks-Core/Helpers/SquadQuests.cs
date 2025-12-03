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
        private static Dictionary<string, JArray> _squadData;

        public static void LoadData(Dictionary<string, string> squadMembers)
        {
            _squadNicks = new Dictionary<string, string>(squadMembers);
            _squadData = JObject.Parse(RequestHandler.PostJson("/all-quests-checkmarks/active-quests", new JArray(_squadNicks.Keys).ToJson()))
                .ToObject<Dictionary<string, JArray>>();
            ParseData();
        }

        public static void ParseData()
        {
            SquadQuestsDict.Clear();

            foreach(KeyValuePair<string, JArray> keyValuePair in _squadData)
            {
                keyValuePair.Deconstruct(out string profileId, out JArray questsData);

                Dictionary<string, bool> playerQuests = new Dictionary<string, bool>();
                SquadQuestsDict.Add(profileId, playerQuests);

                for (int i = 0; i < questsData.Count; ++i)
                {
                    JObject quest = questsData[i] as JObject;
                    string questId = quest["_id"].ToString();

                    if (quest["conditions"] ? ["AvailableForFinish"] == null)
                    {
                        Plugin.LogSource.LogError($"Quest {questId} is missing finish conditions!");
                        continue;
                    }

                    JArray finishConditions = quest["conditions"]["AvailableForFinish"] as JArray;
                    
                    for (int j = 0; j < finishConditions.Count; ++j)
                    {
                        JObject condition = finishConditions[j] as JObject;
                        string conditionType = condition["conditionType"]?.ToString();
                        bool fir = condition["onlyFoundInRaid"]?.ToString() == "True";

                        if (conditionType == null)
                        {
                            Plugin.LogSource.LogError($"Quest {questId} is missing finish condition #{j}!");
                            continue;
                        }

                        if (conditionType != "HandoverItem" && 
                            conditionType != "FindItem" && 
                            conditionType != "LeaveItemAtLocation")
                            continue;

                        if (condition["target"] == null)
                        {
                            Plugin.LogSource.LogError($"Quest {questId} condition #{j} is missing target!");
                            continue;
                        }

                        JArray targets = condition["target"] as JArray;
                        
                        for (int k = 0; k < targets.Count; ++k)
                        {
                            string itemId = targets[k].ToString();
                            
                            if (conditionType != "HandoverItem" && 
                                QuestsHelper.SPECIAL_BLACKLIST.Contains(itemId))
                                continue;

                            if (!playerQuests.TryGetValue(itemId, out bool wasFir) || 
                                (!wasFir && fir))
                                playerQuests[itemId] = fir;
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
                    members.Add(nick);
            }

            return members.Count > 0;
        }
    }
}
