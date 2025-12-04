using AllQuestsCheckmarks.Helpers;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    internal class ItemSpecificationPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EFT.UI.ItemSpecificationPanel), nameof(EFT.UI.ItemSpecificationPanel.method_2));
        }

        [PatchPrefix]
        static bool Prefix(ref QuestItemViewPanel ____questItemViewPanel)
        {
            if (Assets.Checkmark == null || ____questItemViewPanel == null)
            {
                return true;
            }

            try
            {
                FieldInfo info = typeof(QuestItemViewPanel).GetField("_foundInRaidSprite", BindingFlags.NonPublic | BindingFlags.Instance);
                info.SetValue(____questItemViewPanel, Assets.Checkmark);
            }
            catch
            {
                Plugin.LogSource?.LogError("Failed to set custom checkmark in ItemSpecificationPanel!");
            }

            return true;
        }
    }
}
