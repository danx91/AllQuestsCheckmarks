using AllQuestsCheckmarks.Helpers;
using AllQuestsCheckmarks.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.IO;
using System.Reflection;

namespace AllQuestsCheckmarks
{
    [
        BepInPlugin("com.zgfuedkx.allquestscheckmarks", "ZGFueDkx-AllQuestsCheckmarks", "1.4.0"),
        BepInDependency("com.SPT.core", "4.1.0"),
        BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency),
    ]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource? LogSource;
        public static bool isFikaInstalled = false;
        public static string? modPath;

        public void Awake()
        {
            LogSource = Logger;

            modPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Plugin)).Location);
            modPath.Replace("\\", "/");

            isFikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");

            Settings.Init(Config);
            Helpers.Assets.LoadAssets();

            if (isFikaInstalled)
            {
                FikaBridge.Init();
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
            if (Settings.ShowDebug!.Value)
            {
                LogSource?.LogDebug(msg);
            }
        }
    }
}
