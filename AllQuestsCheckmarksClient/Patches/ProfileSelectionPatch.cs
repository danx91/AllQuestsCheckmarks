using AllQuestsCheckmarks.Helpers;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace AllQuestsCheckmarks.Patches
{
    internal class ProfileSelectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class308.Class1596), nameof(Class308.Class1596.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Profile selected");
            QuestsData.LoadData();
        }
    }
}
