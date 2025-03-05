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

        [PatchPrefix]
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

            bool showNonFir = Settings.includeNonFir.Value;
            QuestsHelper.ItemsCount inStash = QuestsHelper.GetItemsInStash(item.TemplateId);
            ___string_5 += string.Format("aqc_in_stash".Localized(null), inStash.total, inStash.fir);

            bool neededForActive = false;
            bool neededForFuture = false;
            bool neededForFriend = false;

            int handedOverFir = 0;
            int handedOverNonFir = 0;

            string activeQuestsTooltip = "";
            string futureQuestsTooltip = "";

            if (QuestsHelper.GetActiveQuestsWithItem(profile, item, out Dictionary<string, QuestsHelper.CurrentQuest> activeQuests,
                out Dictionary<string, QuestsHelper.CurrentQuest> fulfilled))
            {
                foreach (KeyValuePair<string, QuestsHelper.CurrentQuest> keyValuePair in activeQuests)
                {
                    keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                    if(showNonFir || item.QuestItem || quest.condition.onlyFoundInRaid)
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

                    activeQuestsTooltip += $"\n  · <color=#dd831a>{quest.template.Name}</color>: ";

                    if (quest.condition is ConditionHandoverItem condition
                        && profile.TaskConditionCounters.TryGetValue(condition.id, out TaskConditionCounterClass counter))
                    {
                        if (condition.onlyFoundInRaid)
                        {
                            handedOverFir += counter.Value;
                        }
                        else
                        {
                            handedOverNonFir += counter.Value;
                        }

                        activeQuestsTooltip += $"{counter.Value}/{condition.value}";

                        if(showNonFir)
                        {
                            activeQuestsTooltip += " " + (condition.onlyFoundInRaid ? "aqc_fir" : "aqc_nonfir").Localized(null);
                        }
                    }
                    else
                    {
                        activeQuestsTooltip += $"0/{quest.condition.value}";
                    }
                }
            }

            foreach (KeyValuePair<string, QuestsHelper.CurrentQuest> keyValuePair in fulfilled)
            {
                keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                if (quest.condition.onlyFoundInRaid)
                {
                    handedOverFir += (int) quest.condition.value;
                }
                else
                {
                    handedOverNonFir += (int) quest.condition.value;
                }
            }

            if (QuestsHelper.IsNeededForActiveOrFutureQuests(item, out QuestsData.ItemData itemData))
            {
                if(itemData.fir + itemData.nonFir - handedOverFir - handedOverNonFir > 0)
                {
                    ___string_5 += "\n" + string.Format((showNonFir ? "aqc_total_needed_alt" : "aqc_total_needed").Localized(null),
                        itemData.fir - handedOverFir, itemData.nonFir - handedOverNonFir);
                }

                foreach (KeyValuePair<string, QuestsData.QuestValues> quest in itemData.quests)
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

                    string questName = quest.Value.localizedName.Localized(null);
                    if(questName == quest.Value.localizedName)
                    {
                        if (questName.IsNullOrEmpty())
                        {
                            questName = "Unknown Quest";
                        }
                        else
                        {
                            questName = quest.Value.name;
                        }
                    }

                    futureQuestsTooltip += $"\n  · <color=#d24dff>{questName}</color>: {quest.Value.count.count}";

                    if (showNonFir)
                    {
                        futureQuestsTooltip += " " + (quest.Value.count.fir ? "aqc_fir" : "aqc_nonfir").Localized(null);
                    }
                }
            }

            ___string_5 += activeQuestsTooltip + futureQuestsTooltip;

            //TODO: Check if player is in raid? (IsNeededForSquadMembers will return false outside raid anyway, so idk)
            if (Plugin.isFikaInstalled && Settings.squadQuests.Value && SquadQuests.IsNeededForSquadMembers(item, out List<string> members))
            {
                neededForFriend = true;
                ___string_5 += "\n" + "aqc_squad_quests".Localized(null);

                foreach(string nick in members)
                {
                    ___string_5 += $"\n  · <color=#ffc299>{nick}</color>";
                }
            }

            if (neededForActive)
            {
                QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____questItemSprite, QuestsHelper.DEFAULT_COLOR);
            }
            else if(neededForFuture)
            {
                QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite,
                    item.MarkedAsSpawnedInSession ? Settings._checkmarkColor : Settings._nonFirColor);
            }
            else if(neededForFriend)
            {
                QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, Settings._squadColor);
            }
            else if (item.MarkedAsSpawnedInSession)
            {
                QuestsHelper.SetCheckmark(__instance, ____questIconImage, ____foundInRaidSprite, QuestsHelper.DEFAULT_COLOR);
            }

            return false;
        }
    }
}
