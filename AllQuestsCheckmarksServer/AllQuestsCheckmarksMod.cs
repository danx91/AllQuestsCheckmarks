using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;

namespace AllQuestsCheckmarks
{
    [Injectable]
    public class AllQuestsCheckmarksMod(
        HttpResponseUtil httpResponseUtil,
        QuestConfig questConfig,
        ProfileHelper profileHelper,
        QuestHelper questHelper,
        ISptLogger<AllQuestsCheckmarksMod> logger
    )
    {
        private readonly HttpResponseUtil? _httpResponseUtil = httpResponseUtil;
        private readonly QuestConfig _questConfig = questConfig;
        private readonly ProfileHelper? _profileHelper = profileHelper;
        private readonly QuestHelper? _questHelper = questHelper;
        private readonly ISptLogger<AllQuestsCheckmarksMod>? _logger = logger;

        public ValueTask<string> GetAllQuests(MongoId profileId)
        {
            PmcData? profile = _profileHelper!.GetPmcProfile(profileId);

            if (profile is null || profile.Info is not Info profileInfo || profile.Quests is null)
            {
                _logger?.Error($"Failed to retrieve user profile or info: ${profileId}");
                return new ValueTask<string>(_httpResponseUtil!.EmptyResponse());
            }

            List<QuestJson> quests = [];
            List<Quest> allQuests = _questHelper!.GetQuestsFromDb();

            foreach (Quest quest in allQuests)
            {
                if (!_questHelper.ShowEventQuestToPlayer(quest.Id) || !IsQuestForGameType(quest.Id, profileInfo.GameVersion!, _questConfig))
                {
                    QuestJson newQuest = new(quest)
                    {
                        IsUnreachable = true
                    };

                    quests.Add(newQuest);
                    continue;
                }

                if (IsOtherFaction(profile, quest.Id, _questConfig))
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
                if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new QuestJson(quest));
                }
            }

            return new ValueTask<string>(_httpResponseUtil!.NoBody(quests));
        }

        public ValueTask<string> HandleGetActiveQuests(List<MongoId> info)
        {
            Dictionary<MongoId, List<QuestStripped>> data = [];

            foreach (MongoId profileId in info)
            {
                try
                {
                    data[profileId] = GetActiveQuests(profileId);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Error retrieving active quests for profile {profileId}: {ex}");
                    data[profileId] = [];
                }
            }

            return new ValueTask<string>(_httpResponseUtil!.NoBody(data));
        }

        private List<QuestStripped> GetActiveQuests(MongoId profileId)
        {
            List<QuestStripped> quests = [];
            PmcData? profile = _profileHelper!.GetPmcProfile(profileId);

            if (profile is null || profile.Quests is not List<QuestStatus> profileQuests)
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

                if (questStatus.CompletedConditions is null || questStatus.CompletedConditions.Count == 0)
                {
                    quests.Add(new QuestStripped(quest));
                    continue;
                }

                List<QuestCondition> newConditions = quest.Conditions.AvailableForFinish!.FindAll(c => questStatus.CompletedConditions.Contains(c.Id));

                if (newConditions.Count == 0)
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

            if (profile.RepeatableQuests is null)
            {
                return quests;
            }

            foreach (var current in profile.RepeatableQuests)
            {
                if (current.ActiveQuests is null)
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
            bool usec = profile.Info!.Side!.Equals("usec", StringComparison.OrdinalIgnoreCase);
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
