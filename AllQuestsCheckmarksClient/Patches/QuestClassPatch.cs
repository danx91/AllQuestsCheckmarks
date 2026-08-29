using AllQuestsCheckmarks.Helpers;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    class QuestClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EFT.Quests.Quest), nameof(EFT.Quests.Quest.SetStatus));
        }

        [PatchPrefix]
        static bool Prefix(EFT.Quests.Quest __instance, ref EQuestStatus __state)
        {
            __state = __instance.QuestStatus;
            return true;
        }

        [PatchPostfix]
        static void Postfix(EFT.Quests.Quest __instance, EQuestStatus __state)
        {
            if (__instance.QuestStatus == __state)
            {
                return;
            }

            Plugin.LogDebug($"Quest {__instance.Template.Name} status changed from {__state} to {__instance.QuestStatus}");

            switch (__instance.QuestStatus)
            {
                case EQuestStatus.Started:
                    if (__instance is DailyQuest)
                    {
                        Plugin.LogDebug($"Repeatable quest {__instance.Template.Name} accepetd - reload quests");
                        QuestsData.LoadData();
                    }
                    break;
                case EQuestStatus.AvailableForFinish:
                case EQuestStatus.Success:
                case EQuestStatus.Expired:
                case EQuestStatus.Fail:
                    QuestsData.RemoveQuest(__instance.Template.Id);
                    break;
            }
        }
    }
}
