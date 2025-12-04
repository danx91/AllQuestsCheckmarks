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
                        Dictionary<MongoId, List<QuestStripped>> data = [];

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

            List<QuestJson> quests = [];
            List<Quest> allQuests = _questHelper!.GetQuestsFromDb();

            foreach (Quest quest in allQuests)
            {
                if (!_questHelper.ShowEventQuestToPlayer(quest.Id) || !IsQuestForGameType(quest.Id, profileInfo.GameVersion!, questConfig))
                {
                    QuestJson newQuest = new(quest)
                    {
                        IsUnreachable = true
                    };

                    quests.Add(newQuest);
                    continue;
                }

                if (IsOtherFaction(profile, quest.Id, questConfig))
                {
                    continue;
                }

                QuestStatusEnum questStatus = profile.GetQuestStatus(quest.Id);
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

                quests.Add(new QuestJson(quest));
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if(profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new QuestJson(quest));
                }
            }

            return new ValueTask<string>(_jsonUtil!.Serialize(quests)!);
        }

        private static List<QuestStripped> GetActiveQuests(MongoId profileId)
        {
            List<QuestStripped> quests = [];
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
                    quests.Add(new QuestStripped(quest));
                    continue;
                }

                List<QuestCondition> newConditions = quest.Conditions.AvailableForFinish!.FindAll(c => questStatus.CompletedConditions.Contains(c.Id));

                if(newConditions.Count == 0)
                {
                    continue;
                }

                quests.Add(new(quest, availableForFinishCondition: AvailableForFinishCondition.FromQuestConditions(newConditions)));
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new QuestStripped(quest));
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
            bool usec = profile.Info!.Side!.Equals("usec", StringComparison.CurrentCultureIgnoreCase);
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
    }
}
