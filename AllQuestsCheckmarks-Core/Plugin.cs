using System.IO;
using System.Reflection;
using System;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;

using AllQuestsCheckmarks.Helpers;
using AllQuestsCheckmarks.Patches;

namespace AllQuestsCheckmarks
{
    [
        BepInPlugin("ZGFueDkx.AllQuestCheckmarks", "AllQuestsCheckmarks", "1.2.1"),
        BepInDependency("com.SPT.core", "3.11.0"),
        BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency),
        BepInDependency("VIP.TommySoucy.MoreCheckmarks", BepInDependency.DependencyFlags.SoftDependency)
    ]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static bool isFikaInstalled = false;
        public static bool isMoreCheckmarksInstalled = false;
        public static string modPath;

        public void Awake()
        {
            LogSource = Logger;

            modPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Plugin)).Location);
            modPath.Replace("\\", "/");

            isFikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
            isMoreCheckmarksInstalled = Chainloader.PluginInfos.ContainsKey("VIP.TommySoucy.MoreCheckmarks");

            Settings.Init(Config);
            Assets.LoadAssets();

            if (isFikaInstalled)
            {
                FikaBridge.Init();
            }
            else
            {
                new LocalGameStartPatch().Enable();
            }

            if (isMoreCheckmarksInstalled)
            {
                MoreCheckmarksBridge.Init();
            }

            new QuestClassPatch().Enable();
            new ProfileSelectionPatch().Enable();
            new QuestItemViewPanelPatch().Enable();
            new ItemSpecificationPanelPatch().Enable();
            new UpdateApplicationLanguagePatch().Enable();

            LogSource.LogInfo($"AllQuestCheckmarks by ZGFueDkx version {Info.Metadata.Version} started");
        }

        public static void LogDebug(string msg)
        {
            if (!Settings.ShowDebug.Value)
            {
                return;
            }

            LogSource.LogDebug(msg);
        }
    }
}
