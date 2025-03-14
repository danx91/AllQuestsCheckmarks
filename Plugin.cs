using System.IO;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;

using AllQuestsCheckmarks.Helpers;
using AllQuestsCheckmarks.Patches;

namespace AllQuestsCheckmarks
{
    [
        BepInPlugin("ZGFueDkx.AllQuestCheckmarks", "AllQuestsCheckmarks", "1.1.0"),
        BepInDependency("com.SPT.core", "3.10.5"),
        BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency),
        BepInIncompatibility("VIP.TommySoucy.MoreCheckmarks")
    ]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static bool isFikaInstalled = false;
        public static string modPath;

        public void Awake()
        {
            LogSource = Logger;
            isFikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
            modPath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Plugin)).Location);
            modPath.Replace("\\", "/");

            Settings.Init(Config);
            Assets.LoadAssets();

            if (isFikaInstalled)
            {
                FikaEventSubscriber.Init();
            }
            else
            {
                new LocalGameStartPatch().Enable();
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
            if (!Settings.showDebug.Value)
            {
                return;
            }

            LogSource.LogDebug(msg);
        }
    }
}
