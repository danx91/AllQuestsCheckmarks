using AllQuestsCheckmarks.Helpers;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    class UpdateApplicationLanguagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocaleManagerClass), nameof(LocaleManagerClass.UpdateApplicationLanguage));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Locales.LoadLocale(LocaleManagerClass.LocaleManagerClass.String_0);
        }
    }
}
