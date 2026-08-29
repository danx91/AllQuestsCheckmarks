using AllQuestsCheckmarks.Helpers;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    internal class ProfileSelectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EftClientBackendSession.CG_SetMainProfile), nameof(EftClientBackendSession.CG_SetMainProfile.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Profile selected");
            QuestsData.LoadData();
        }
    }
}
