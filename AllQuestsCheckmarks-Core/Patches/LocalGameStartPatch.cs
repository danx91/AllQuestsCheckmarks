﻿using System.Reflection;

using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

using AllQuestsCheckmarks.Helpers;

namespace AllQuestsCheckmarks.Patches
{
    internal class LocalGameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), nameof(LocalGame.smethod_6));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Local game started");
            StashHelper.BuildItemsCache();
        }
    }
}
