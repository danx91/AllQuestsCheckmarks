using System.Collections;
using System.Text.Json;
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

namespace AllQuestCheckmarks_Server;

[Injectable]
public class CustomStaticRouter : StaticRouter
{
    private static QuestHelper? _questHelper;
    private static ConfigServer? _configServer;
    private static ProfileHelper? _profileHelper;
    private static HttpResponseUtil? _httpResponseUtil;
    private static ISptLogger<AllQuestCheckmarksServer>? _logger;
    private static JsonUtil? _jsonUtil;

    public CustomStaticRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil) : base(
        jsonUtil,
        GetCustomRoutes())
    {
        _jsonUtil = jsonUtil;
        _httpResponseUtil = httpResponseUtil;
    }

    public void PassData(QuestHelper questHelper, ConfigServer configServer, ProfileHelper profileHelper, ISptLogger<AllQuestCheckmarksServer> logger)
    {
        _questHelper = questHelper;
        _configServer = configServer;
        _profileHelper = profileHelper;
        _logger = logger;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<EmptyRequestData>(
                "/all-quests-checkmarks/quests",
                static async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await HandleGetAllQuest(sessionId)
            ), 
            new RouteAction(
                "/all-quests-checkmarks/active-quests",
                static async (
                    url, 
                    info, 
                    sessionId, 
                    output
                ) =>
                {
                    Dictionary<MongoId, List<Quest>> data = [];
                    
                    foreach (MongoId profileId in (IEnumerable)info)
                    {
                        data[profileId] = GetActiveQuest(profileId);
                    }

                    return new ValueTask<string>(_jsonUtil!.Serialize(data)!);
                }
            )
        ];
    }

    private static ValueTask<string> HandleGetAllQuest(MongoId sessionId)
    {
        try
        {
            List<Quest2> quests = GetAllQuests(sessionId);
            return new ValueTask<string>(_jsonUtil!.Serialize(quests)!);
        }
        catch
        {
            _logger.Error("Exception when handling QuestsRoute!");
            return new ValueTask<string>(_httpResponseUtil!.NullResponse());
        }
    }

    private static List<Quest2> GetAllQuests(MongoId sessionId)
    {
        QuestConfig questConfig = _configServer.GetConfig<QuestConfig>();
        List<Quest> allQuestsTmp = _questHelper.GetQuestsFromDb();
        List<Quest2> allQuests = allQuestsTmp.Select(ToQuest2).ToList();
        PmcData profile = _profileHelper.GetPmcProfile(sessionId);
        List<Quest2> quests = [];

        if (profile == null ||
            profile.Quests == null)
        {
            _logger.Error("Failed to retrieve user profile or quests: " + sessionId);
            return quests;
        }

        foreach (Quest2 quest in allQuests.ToList())
        {
            if (!_questHelper.ShowEventQuestToPlayer(quest.Base.Id) ||
                !IsQuestForGameType(quest.Base.Id, profile.Info.GameVersion, questConfig))
            {
                Quest2 tmpQuest = StructuredClone(quest);
                tmpQuest.IsUnreachable = true;
                quests.Add(tmpQuest);
                continue;
            }
            
            if (IsOtherFaction(profile, quest.Base.Id, questConfig))
                continue;

            QuestStatusEnum questStatus = profile.GetQuestStatus(quest.Base.Id);
            
            if (questStatus is QuestStatusEnum.AvailableForFinish or 
                                QuestStatusEnum.Success or 
                                QuestStatusEnum.Fail or 
                                QuestStatusEnum.FailRestartable or 
                                QuestStatusEnum.MarkedAsFailed or 
                                QuestStatusEnum.Expired)
                continue;
            
            quests.Add(quest);
        }

        List<RepeatableQuest> repeatableQuests = GetRepeatableQuests(profile);
        List<Quest2> repeatableQuests2 = repeatableQuests.Select(ToQuest2).ToList();

        foreach (Quest2 quest in repeatableQuests2)
        {
            if (profile.GetQuestStatus(quest.Base.Id) == QuestStatusEnum.Started)
                quests.Add(quest);
        }
        
        return quests;
    }

    private static List<Quest> GetActiveQuest(MongoId sessionId)
    {
        PmcData profile = _profileHelper.GetPmcProfile(sessionId);
        List<Quest> quests = [];

        if (profile == null ||
            profile.Info == null)
        {
            _logger.Error("Failed to retrieve user profile or quests: " + sessionId);
            return quests;
        }

        List<Quest> allQuests = _questHelper.GetClientQuests(sessionId);
        
        foreach (Quest quest in allQuests.ToList())
        {
            QuestStatus questData = profile.Quests.Find(q => q.QId == quest.Id);

            if (questData == null ||
                questData.Status != QuestStatusEnum.Started) 
                continue;
            
            if (questData.CompletedConditions?.Count > 0)
            {
                List<QuestCondition> tmpConditions =
                    quest.Conditions.AvailableForFinish!.FindAll(condition =>
                        !questData.CompletedConditions.Contains(condition.Id));

                if (tmpConditions.Count <= 0) 
                    continue;
                    
                var tmpQuest = StructuredClone(quest);
                tmpQuest.Conditions.AvailableForFinish = tmpConditions;
                        
                allQuests.Add(tmpQuest);
            }
            else
            {
                allQuests.Add(quest);
            }
        }

        foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
        {
            if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                allQuests.Add(quest);
        }

        return allQuests;
    }

    private static List<RepeatableQuest> GetRepeatableQuests(PmcData profile)
    {
        List<RepeatableQuest> quests = [];

        if (profile.RepeatableQuests == null)
            return [];

        foreach (var current in profile.RepeatableQuests.ToList())
        {
            if (current.ActiveQuests == null)
                return [];
            
            foreach (var quest in current.ActiveQuests.ToList())
            {
                quests.Add(quest);
            }
        }
        
        return quests;
    }

    private static bool IsQuestForGameType(string questId, string version, QuestConfig questConfig)
    {
        if (!questConfig.ProfileBlacklist.TryGetValue(version, out var questBlacklist))
            return false;
        
        if (questBlacklist?.Count > 0 &&
            questBlacklist.Contains(questId))
            return false;

        if (!questConfig.ProfileWhitelist.TryGetValue(questId, out var questWhitelist))
            return false;

        return questWhitelist.Count <= 0 ||
               questWhitelist.Contains(version);
    }

    private static bool IsOtherFaction(PmcData profile, string questId, QuestConfig questConfig)
    {
        bool usec = profile.Info.Side.ToLower() == "usec";
        return usec && questConfig.BearOnlyQuests.Contains(questId) || 
               !usec && questConfig.UsecOnlyQuests.Contains(questId);
    }
    
    private static T StructuredClone<T>(T obj)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj))!;
    }
    
    private static Quest2 ToQuest2(Quest q)
    {
        return new Quest2
        {
            Base = q,
            IsUnreachable = false
        };
    }

    private record Quest2
    {
        public Quest Base { get; init; }
        public bool? IsUnreachable { get; set; }
    }
}