using System.Collections.Generic;
using Newtonsoft.Json.Linq;

using EFT.InventoryLogic;
using SPT.Common.Http;
using Fika.Core.Coop.Players;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class SquadQuests
    {
        public static Dictionary<string, Dictionary<string, bool>> squadQuests = new Dictionary<string, Dictionary<string, bool>>();
        private static Dictionary<string, string> squadNicks = new Dictionary<string, string>();
        private static Dictionary<string, JArray> squadData;

        public static void LoadData(List<CoopPlayer> squadMembers)
        {
            squadNicks.Clear();

            foreach(CoopPlayer player in squadMembers)
            {
                squadNicks.Add(player.ProfileId, player.Profile.Nickname);
            }

            squadData = JObject.Parse(RequestHandler.PostJsonAsync("/all-quests-checkmarks/active-quests", new JArray(squadNicks.Keys).ToJson()))
                .ToObject<Dictionary<string, JArray>>();
            ParseData();
        }

        public static void ParseData()
        {
            squadQuests.Clear();

            foreach(KeyValuePair<string, JArray> keyValuePair in squadData)
            {
                keyValuePair.Deconstruct(out string profileId, out JArray questsData);

                Dictionary<string, bool> playerQuests = new Dictionary<string, bool>();
                squadQuests.Add(profileId, playerQuests);

                for (int i = 0; i < questsData.Count; ++i)
                {
                    JObject quest = questsData[i] as JObject;
                    string questId = quest["_id"].ToString();

                    if (quest["conditions"] == null || quest["conditions"]["AvailableForFinish"] == null)
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
                        else if (conditionType != "HandoverItem" && conditionType != "FindItem" && conditionType != "LeaveItemAtLocation")
                        {
                            continue;
                        }
                        else if (condition["target"] == null)
                        {
                            Plugin.LogSource.LogError($"Quest {questId} condition #{j} is missing target!");
                            continue;
                        }

                        JArray targets = condition["target"] as JArray;
                        for (int k = 0; k < targets.Count; ++k)
                        {
                            string itemId = targets[k].ToString();
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
            squadQuests.Clear();
        }

        public static bool IsNeededForSquadMembers(Item item, out List<string> members)
        {
            members = new List<string>();

            foreach(KeyValuePair<string, Dictionary<string, bool>> keyValuePair in squadQuests)
            {
                keyValuePair.Deconstruct(out string profileId, out Dictionary<string, bool> items);

                if (items.TryGetValue(item.TemplateId, out bool fir) && (!fir || item.MarkedAsSpawnedInSession) && squadNicks.TryGetValue(profileId, out string nick))
                {
                    members.Add(nick);
                }
            }

            return members.Count > 0;
        }
    }
}
