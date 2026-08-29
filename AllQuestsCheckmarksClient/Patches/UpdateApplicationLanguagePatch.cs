using AllQuestsCheckmarks.Helpers;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    class UpdateApplicationLanguagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalizationManager), nameof(LocalizationManager.UpdateApplicationLanguage));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Locales.LoadLocale(LocalizationManager.Instance.Culture);
        }
    }
}
