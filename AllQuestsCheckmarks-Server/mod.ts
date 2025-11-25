import { DependencyContainer } from "tsyringe";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { QuestHelper } from "@spt/helpers/QuestHelper";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { IQuestConfig } from "@spt/models/spt/config/IQuestConfig";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import { IQuest } from "@spt/models/eft/common/tables/IQuest";
import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { QuestStatus } from "@spt/models/enums/QuestStatus";
import { IRepeatableQuest } from "@spt/models/eft/common/tables/IRepeatableQuests";

type IQuest2 = IQuest & { isUnreachable?: boolean };

class Mod implements IPreSptLoadMod
{

    private static container: DependencyContainer;

    public preSptLoad(container: DependencyContainer): void
    {
        Mod.container = container;
        const staticRouter = container.resolve<StaticRouterModService>("StaticRouterModService");

        staticRouter.registerStaticRouter("AllQuestsCheckmarksModStaticRouter", 
            [
                {
                    url: "/all-quests-checkmarks/quests",
                    action: async (url, info, sessionID) =>
                    {
                        return JSON.stringify(this.getAllQuests(sessionID));
                    }
                },
                {
                    url: "/all-quests-checkmarks/active-quests",
                    action: async (url, info) =>
                    {
                        const data = {};

                        for (const profileId of info)
                        {
                            data[profileId] = this.getActiveQuests(profileId);
                        }

                        return JSON.stringify(data);
                    }
                }
            ],
            "custom-static-all-quests-checkmarks"
        )
    }

    private getAllQuests(sessionId: string): IQuest2[]
    {
        const logger: ILogger = Mod.container.resolve<ILogger>("WinstonLogger");
        const profileHelper: ProfileHelper = Mod.container.resolve<ProfileHelper>("ProfileHelper");
        const questHelper: QuestHelper = Mod.container.resolve<QuestHelper>("QuestHelper");
        const configServer: ConfigServer = Mod.container.resolve<ConfigServer>("ConfigServer");

        const questConfig: IQuestConfig = configServer.getConfig<IQuestConfig>(ConfigTypes.QUEST);

        const quests: IQuest2[] = [];
        const profile: IPmcData = profileHelper.getPmcProfile(sessionId);

        if (!profile || !profile.Quests)
        {
            logger.error(`Failed to retrieve user profile or quests: ${sessionId}!`);
            return quests;
        }

        const allQuests: IQuest2[] = questHelper.getQuestsFromDb();

        for (const quest of allQuests)
        {
            if (!questHelper.showEventQuestToPlayer(quest._id) || !this.isQuestForGameType(quest._id, profile.Info.GameVersion, questConfig))
            {
                const tmpQuest = structuredClone(quest);
                tmpQuest.isUnreachable = true;
                quests.push(tmpQuest);
                continue;
            }

            if (this.isOtherFaction(profile, quest._id, questConfig))
            {
                continue;
            }

            const status: QuestStatus = questHelper.getQuestStatus(profile, quest._id);
            if (status > 2 && status < 9)
            {
                continue;
            }

            quests.push(quest);
        }

        for (const quest of this.getRepeatableQuests(profile))
        {
            if (questHelper.getQuestStatus(profile, quest._id) == QuestStatus.Started)
            {
                quests.push(quest);
            }
        }

        //logger.info(`Got quests for session: ${sessionId}`);
        return quests;
    }

    private getActiveQuests(sessionId: string): IQuest[]
    {
        const logger: ILogger = Mod.container.resolve<ILogger>("WinstonLogger");
        const questHelper: QuestHelper = Mod.container.resolve<QuestHelper>("QuestHelper");
        const profileHelper: ProfileHelper = Mod.container.resolve<ProfileHelper>("ProfileHelper");

        const quests: IQuest[] = [];
        const profile: IPmcData = profileHelper.getPmcProfile(sessionId);

        if (!profile || !profile.Info)
        {
            logger.error(`Failed to retrieve user profile or info: ${sessionId}!`);
            return quests;
        }

        const allQuests: IQuest[] = questHelper.getClientQuests(sessionId);

        for (const quest of allQuests)
        {
            const questData = profile.Quests?.find((q) => q.qid === quest._id);

            //if (questHelper.getQuestStatus(profile, quest._id) == QuestStatus.Started)
            if (questData && questData.status == QuestStatus.Started)
            {
                if (questData.completedConditions && questData.completedConditions.length > 0)
                {
                    const tmpConditions = quest.conditions.AvailableForFinish.filter((condition) => !questData.completedConditions.includes(condition.id));

                    if (tmpConditions.length > 0)
                    {
                        const tmpQuest = structuredClone(quest);
                        tmpQuest.conditions.AvailableForFinish = tmpConditions;
                        quests.push(tmpQuest);
                    }
                }
                else
                {
                    quests.push(quest);
                }
            }
        }

        for (const quest of this.getRepeatableQuests(profile))
        {
            if (questHelper.getQuestStatus(profile, quest._id) == QuestStatus.Started)
            {
                quests.push(quest);
            }
        }

        //logger.info(`Got active quests of: ${sessionId}`);
        return quests;
    }

    private getRepeatableQuests(profile: IPmcData): IRepeatableQuest[]
    {
        const quests: IRepeatableQuest[] = [];

        for (const current of profile.RepeatableQuests)
        {
            for (const quest of current.activeQuests)
            {
                quests.push(quest);
            }
        }

        return quests;
    }

    //Check if quest if for other faction
    private isOtherFaction(profile: IPmcData, questId: string, questConfig: IQuestConfig): boolean
    {
        const usec = profile.Info.Side.toLowerCase() === "usec";
        return usec && questConfig.bearOnlyQuests.includes(questId) || !usec && questConfig.usecOnlyQuests.includes(questId);
    }
    
    //Contains copied code from QuestHelpers.ts
    private isQuestForGameType(questId: string, version: string, questConfig: IQuestConfig): boolean
    {
        const questBlacklist = questConfig.profileBlacklist[version];
        if (questBlacklist && questBlacklist.includes(questId))
        {
            return false;
        }

        const questWhitelist = questConfig.profileWhitelist[questId];
        if (questWhitelist && !questWhitelist.includes(version))
        {
            return false;
        }

        return true;
    }
}

export const mod = new Mod();
