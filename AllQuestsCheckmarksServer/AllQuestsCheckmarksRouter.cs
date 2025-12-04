using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace AllQuestsCheckmarks
{
    [Injectable]
    internal class AllQuestsCheckmarksRouter : StaticRouter
    {
        private class ActiveQuestsRequestData : List<MongoId>, IRequestData;
        private class Quest2(Quest quest)
        {
            public Quest Quest { get; set; } = quest;
            public bool? IsUnreachable { get; set; }
        }

        private static JsonUtil? _jsonUtil;
        private static HttpResponseUtil? _httpResponseUtil;
        private static ConfigServer? _configServer;
        private static ProfileHelper? _profileHelper;
        private static QuestHelper? _questHelper;
        private static ISptLogger<AllQuestsCheckmarksMod>? _logger;

        public AllQuestsCheckmarksRouter(
            JsonUtil jsonUtil,
            HttpResponseUtil httpResponseUtil,
            ConfigServer configServer,
            ProfileHelper profileHelper,
            QuestHelper questHelper,
            ISptLogger<AllQuestsCheckmarksMod> logger
        ) : base(jsonUtil, GetRoutes())
        {
            _jsonUtil = jsonUtil;
            _httpResponseUtil = httpResponseUtil;
            _configServer = configServer;
            _profileHelper = profileHelper;
            _questHelper = questHelper;
            _logger = logger;
        }

        private static List<RouteAction> GetRoutes()
        {
            return
            [
                new RouteAction<EmptyRequestData>(
                    "/all-quests-checkmarks/quests",
                    (url, info, sessionId, output) => GetAllQuests(sessionId)
                ),
                new RouteAction<ActiveQuestsRequestData>(
                    "/all-quests-checkmarks/active-quests",
                    (url, info, sessionId, output) => {
                        Dictionary<MongoId, List<Quest>> data = [];

                        foreach (MongoId profileId in info) {
                            data[profileId] = GetActiveQuests(profileId);
                        }

                        return new ValueTask<string>(_jsonUtil!.Serialize(data)!);
                    }
                ),
            ];
        }

        public static ValueTask<string> GetAllQuests(MongoId profileId)
        {
            PmcData? profile = _profileHelper!.GetPmcProfile(profileId);

            if (profile == null || profile.Info is not Info profileInfo || profile.Quests is not List<QuestStatus> profileQuests)
            {
                _logger?.Error($"Failed to retrieve user profile or info: ${profileId}");
                return new ValueTask<string>(_httpResponseUtil!.EmptyResponse());
            }

            QuestConfig questConfig = _configServer!.GetConfig<QuestConfig>();

            List<Quest2> quests = [];
            List<Quest2> allQuests = [.. _questHelper!.GetQuestsFromDb().Select(q => new Quest2(q))];

            foreach (Quest2 quest in allQuests)
            {
                if (!_questHelper.ShowEventQuestToPlayer(quest.Quest.Id) || !IsQuestForGameType(quest.Quest.Id, profileInfo.GameVersion!, questConfig))
                {
                    quest.IsUnreachable = true;
                    quests.Add(quest);
                    continue;
                }

                if (IsOtherFaction(profile, quest.Quest.Id, questConfig))
                {
                    continue;
                }

                QuestStatusEnum questStatus = profile.GetQuestStatus(quest.Quest.Id);
                if (questStatus is
                    QuestStatusEnum.AvailableForFinish or
                    QuestStatusEnum.Success or
                    QuestStatusEnum.Fail or
                    QuestStatusEnum.FailRestartable or
                    QuestStatusEnum.MarkedAsFailed or
                    QuestStatusEnum.Expired)
                {
                    continue;
                }

                quests.Add(quest);
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if(profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new Quest2(quest));
                }
            }

            return new ValueTask<string>(_jsonUtil!.Serialize(quests)!);
        }

        private static List<Quest> GetActiveQuests(MongoId profileId)
        {
            List<Quest> quests = [];
            PmcData? profile = _profileHelper!.GetPmcProfile(profileId);

            if(profile == null || profile.Quests is not List<QuestStatus> profileQuests)
            {
                _logger?.Error($"Failed to retrieve user profile or info: ${profileId}");
                return quests;
            }

            List<Quest> allQuests = _questHelper!.GetClientQuests(profileId);

            foreach (Quest quest in allQuests)
            {
                QuestStatus? questStatus = profileQuests.Find(q => q.QId == quest.Id);
                if (questStatus?.Status != QuestStatusEnum.Started)
                {
                    continue;
                }

                if (questStatus.CompletedConditions == null || questStatus.CompletedConditions.Count == 0)
                {
                    quests.Add(quest);
                    continue;
                }

                List<QuestCondition> newConditions = quest.Conditions.AvailableForFinish!.FindAll(c => questStatus.CompletedConditions.Contains(c.Id));

                if(newConditions.Count == 0)
                {
                    continue;
                }

                Quest newQuest = Clone(quest);
                newQuest.Conditions.AvailableForFinish = newConditions;
                quests.Add(newQuest);
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(quest);
                }
            }

            return quests;
        }

        private static List<RepeatableQuest> GetRepeatableQuests(PmcData profile)
        {
            List<RepeatableQuest> quests = [];

            if (profile.RepeatableQuests == null)
            {
                return quests;
            }

            foreach (var current in profile.RepeatableQuests)
            {
                if (current.ActiveQuests == null)
                {
                    continue;
                }

                foreach (var quest in current.ActiveQuests)
                {
                    quests.Add(quest);
                }
            }

            return quests;
        }

        private static bool IsOtherFaction(PmcData profile, MongoId questId, QuestConfig questConfig)
        {
            bool usec = profile.Info!.Side!.ToLower() == "usec";
            return usec && questConfig.BearOnlyQuests.Contains(questId) ||
                   !usec && questConfig.UsecOnlyQuests.Contains(questId);
        }

        private static bool IsQuestForGameType(MongoId questId, string version, QuestConfig questConfig)
        {
            if (questConfig.ProfileBlacklist.TryGetValue(version, out var blacklistValue) && blacklistValue.Contains(questId))
            {
                return false;
            }

            if (questConfig.ProfileWhitelist.TryGetValue(questId, out var whitelistValue) && !whitelistValue.Contains(version))
            {
                return false;
            }

            return true;
        }

        private static T Clone<T>(T source)
        {
            return _jsonUtil!.Deserialize<T>(_jsonUtil!.Serialize(source)!)!;
        }
    }
}
