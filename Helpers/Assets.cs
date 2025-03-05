﻿using UnityEngine;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Assets
    {
        public static Sprite checkmark;

        public static void LoadAssets()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(Plugin.modPath + "/AllQuestsCheckmarksAssets");

            if(bundle == null)
            {
                Plugin.LogSource.LogError("Failed to load asset bundle!");
                return;
            }

            checkmark = bundle.LoadAsset<Sprite>("checkmark");

            Plugin.LogSource.LogInfo("Assets loaded");
        }
    }

}
