using System.Collections.Generic;
using System.Reflection;

using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using AllQuestsCheckmarks.Helpers;

namespace AllQuestsCheckmarks.Patches
{
    internal class QuestItemViewPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestItemViewPanel), nameof(QuestItemViewPanel.Show));
        }

        [
            PatchPrefix,
            HarmonyAfter("VIP.TommySoucy.MoreCheckmarks")
        ]
        static bool Prefix(Profile profile, Item item, [CanBeNull] SimpleTooltip tooltip, QuestItemViewPanel __instance, Image ____questIconImage,
            Sprite ____foundInRaidSprite, Sprite ____questItemSprite, ref string ___string_5, ref SimpleTooltip ___simpleTooltip_0, TextMeshProUGUI ____questItemLabel)
        {
            __instance.HideGameObject();

            if (____questItemLabel != null)
            {
                ____questItemLabel.gameObject.SetActive(item.QuestItem);
                if (item.QuestItem)
                {
                    ____questItemLabel.text = "QUEST ITEM".Localized(null);
                }
            }

            ___simpleTooltip_0 = tooltip;
            ___string_5 = "";

            if (item.MarkedAsSpawnedInSession)
            {
                ___string_5 = "Item found in raid".Localized(null) + "\n";
            }

            bool showNonFir = Settings.IncludeNonFir.Value;
            StashHelper.ItemsCount inStash = StashHelper.GetItemsInStash(item.TemplateId);
            ___string_5 += string.Format("aqc_in_stash".Localized(null), inStash.Total, inStash.Fir);

            bool neededForActive = false;
            bool neededForFuture = false;
            bool neededForFriend = false;
            bool collectorOnly = true;

            int totalNeededFir = 0;
            int totalNeededNonFir = 0;
            int handedOverFir = 0;
            int handedOverNonFir = 0;

            string activeQuestsTooltip = "";
            string futureQuestsTooltip = "";

            bool useCustomTextColors = Settings.CustomTextColors.Value;
            string indent = Settings.BulletPoints.Value ? "  · " : "  ";

            if (QuestsHelper.GetActiveQuestsWithItem(profile, item, out Dictionary<string, QuestsHelper.CurrentQuest> activeQuests,
                out Dictionary<string, QuestsHelper.CurrentQuest> fulfilled))
            {
                string activeColor = useCustomTextColors ? Settings.ActiveQuestTextColor.Hex : "#dd831a";

                foreach (KeyValuePair<string, QuestsHelper.CurrentQuest> keyValuePair in activeQuests)
                {
                    keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                    if(showNonFir || item.QuestItem || quest.Condition.onlyFoundInRaid)
                    {
                        if (!neededForActive)
                        {
                            neededForActive = true;
                            activeQuestsTooltip += "\n" + "aqc_active_quests".Localized(null);
                        }
                    }
                    else
                    {
                        continue;
                    }

                    activeQuestsTooltip += $"\n{indent}<color={activeColor}>{quest.Template.Name}</color>: ";

                    if (quest.Condition is ConditionHandoverItem condition
                        && profile.TaskConditionCounters.TryGetValue(condition.id, out TaskConditionCounterClass counter))
                    {
                        if (condition.onlyFoundInRaid)
                        {
                            handedOverFir += counter.Value;
                            totalNeededFir += (int) condition.value - counter.Value;
                        }
                        else
                        {
                            handedOverNonFir += counter.Value;
                            totalNeededNonFir += (int) condition.value - counter.Value;
                        }

                        activeQuestsTooltip += $"{counter.Value}/{condition.value}";

                        if(showNonFir)
                        {
                            activeQuestsTooltip += " " + (condition.onlyFoundInRaid ? "aqc_fir" : "aqc_nonfir").Localized(null);
                        }
                    }
                    else
                    {
                        activeQuestsTooltip += $"0/{quest.Condition.value}";
                    }
                }
            }

            foreach (KeyValuePair<string, QuestsHelper.CurrentQuest> keyValuePair in fulfilled)
            {
                keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                if (quest.Condition.onlyFoundInRaid)
                {
                    handedOverFir += (int) quest.Condition.value;
                }
                else
                {
                    handedOverNonFir += (int) quest.Condition.value;
                }
            }

            if (QuestsHelper.IsNeededForActiveOrFutureQuests(item, out QuestsData.ItemData itemData))
            {
                string futureColor = useCustomTextColors ? Settings.FutureQuestTextColor.Hex : "#d24dff";

                totalNeededFir = itemData.Fir - handedOverFir;
                totalNeededNonFir = itemData.NonFir - handedOverNonFir;

                foreach (KeyValuePair<string, QuestsData.QuestValues> quest in itemData.Quests)
                {
                    if (activeQuests.ContainsKey(quest.Key) || fulfilled.ContainsKey(quest.Key))
                    {
                        continue;
                    }

                    if (!neededForFuture)
                    {
                        neededForFuture = true;
                        futureQuestsTooltip = "\n" + "aqc_future_quests".Localized();
                    }

                    if(quest.Key != QuestsHelper.COLLECTOR_ID)
                    {
                        collectorOnly = false;
                    }

                    string questName = quest.Value.LocalizedName.Localized(null);
                    if(questName == quest.Value.LocalizedName)
                    {
                        if (questName.IsNullOrEmpty())
                        {
                            questName = "Unknown Quest";
                        }
                        else
                        {
                            questName = quest.Value.Name;
                        }
                    }

                    futureQuestsTooltip += $"\n{indent}<color={futureColor}>{questName}</color>: {quest.Value.Count.count}";

                    if (showNonFir)
                    {
                        futureQuestsTooltip += " " + (quest.Value.Count.fir ? "aqc_fir" : "aqc_nonfir").Localized(null);
                    }
                }
            }

            bool neededForHideout = false;
            bool neededForBarter = false;

            string hideoutTooltip = "";
            string barterTooltip = "";

            if (Plugin.isMoreCheckmarksInstalled)
            {
                if (Settings.MoreCheckmarksHideout.Value
                    && MoreCheckmarksBridge.InvokeGetHideoutAreasEvent(item.TemplateId, out int hideoutNeeded, out List<string> areas))
                {
                    neededForHideout = true;
                    hideoutTooltip = "\n" + string.Format("aqc_hideout".Localized(null), hideoutNeeded);

                    if(Settings.MoreCheckmarksHideoutIncludeTotal.Value)
                    {
                        //TODO: make money non-fir
                        totalNeededFir += hideoutNeeded;
                    }

                    foreach(string area in areas)
                    {
                        hideoutTooltip += $"\n{indent}{area}";
                    }
                }

                if(Settings.MoreCheckmarksBarters.Value
                    && MoreCheckmarksBridge.InvokeGetBartersEvent(item.TemplateId, out List<MoreCheckmarksBridge.TraderBarters> traders))
                {
                    neededForBarter = true;
                    barterTooltip = "\n" + "aqc_barters".Localized(null);
                    string barterColor = Settings.MoreCheckmarksBartersColor.Hex;

                    foreach(MoreCheckmarksBridge.TraderBarters trader in traders)
                    {
                        barterTooltip += $"\n{indent}{trader.TraderName.Localized(null)}:";

                        foreach(MoreCheckmarksBridge.BarterData barter in trader.Barters)
                        {
                            barterTooltip += $"\n  {indent}<color={barterColor}>{barter.BarterName.Localized(null)}</color>: {barter.Count}";
                        }
                    }
                }
            }

            if(totalNeededFir > 0 || showNonFir && totalNeededNonFir > 0)
            {
                ___string_5 += "\n" + string.Format((showNonFir ? "aqc_total_needed_alt" : "aqc_total_needed").Localized(null),
                       totalNeededFir, totalNeededNonFir);
            }

            ___string_5 += activeQuestsTooltip + futureQuestsTooltip;

            if (Plugin.isFikaInstalled && Settings.SquadQuests.Value && SquadQuests.IsNeededForSquadMembers(item, out List<string> members))
            {
                string squadColor = useCustomTextColors ? Settings.SquadQuestTextColor.Hex : "#ffc299";

                neededForFriend = true;
                ___string_5 += "\n" + "aqc_squad_quests".Localized(null);

                foreach(string nick in members)
                {
                    ___string_5 += $"\n{indent}<color={squadColor}>{nick}</color>";
                }
            }

            ___string_5 += hideoutTooltip + barterTooltip;

            int leftFir = inStash.Fir - totalNeededFir;
            switch(QuestsHelper.GetCheckmarkStatus(neededForActive, neededForFuture, neededForFriend, item.MarkedAsSpawnedInSession,
                leftFir >= 0 && inStash.NonFir + leftFir - totalNeededNonFir >= 0, collectorOnly))
            {
                case QuestsHelper.ECheckmarkStatus.Fir:
                    if(neededForHideout || neededForBarter && Settings.MoreCheckmarksBartersCheckmark.Value)
                    {
                        goto case QuestsHelper.ECheckmarkStatus.None;
                    }

                    QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, QuestsHelper.DEFAULT_COLOR);
                    break;
                case QuestsHelper.ECheckmarkStatus.Active:
                    if (Settings.UseCustomQuestColor.Value)
                    {
                        QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.CustomQuestColor.Color);
                    }
                    else
                    {
                        QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____questItemSprite, QuestsHelper.DEFAULT_COLOR);
                    }
                    break;
                case QuestsHelper.ECheckmarkStatus.Future:
                    QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite,
                        item.MarkedAsSpawnedInSession ? Settings.CheckmarkColor.Color : Settings.NonFirColor.Color);
                    break;
                case QuestsHelper.ECheckmarkStatus.Squad:
                    QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.SquadColor.Color);
                    break;
                case QuestsHelper.ECheckmarkStatus.Fulfilled:
                    QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.EnoughItemsColor.Color);
                    break;
                case QuestsHelper.ECheckmarkStatus.Collector:
                    QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.CollectorColor.Color);
                    break;
                case QuestsHelper.ECheckmarkStatus.None:
                    if (neededForHideout)
                    {
                        QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.MoreCheckmarksHideoutColor.Color);
                    }
                    else if (neededForBarter && Settings.MoreCheckmarksBartersCheckmark.Value)
                    {
                        QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings.MoreCheckmarksBartersColor.Color);
                    }
                    break;
            }

            return false;
        }
    }
}
