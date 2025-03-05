using System.Reflection;

using HarmonyLib;
using SPT.Reflection.Patching;

using AllQuestsCheckmarks.Helpers;

namespace AllQuestsCheckmarks.Patches
{
    internal class ProfileSelectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class301.Class1438), nameof(Class301.Class1438.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Profile selected");
            QuestsData.LoadData();
        }
    }
}
