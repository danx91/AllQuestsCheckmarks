using System.IO;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;

using AllQuestsCheckmarks.Helpers;
using AllQuestsCheckmarks.Patches;

/* TODO:
 *  Hide chemark fulfilled - respect squad OK
 *  Update on take item - like quests
 *  
 * FIX:
 *  LeaveItem not shown as active quest
 *  Item condition - durability
 *  Multiple of same item (Sew it good)
 *  
 * DONE:
 *  Custom quest color
 *  Custom text colors
 *  Custom collector color
 *  No bullte points
 *  Count items in ho
 *  Count items on pmc
 *  Moving items removes from count [BUG]
 *  Fulfilled color + Settings OK
 *  Settings - remove cm if total met
 *  Better cache - better cache BUG: Stash areas = nonfir - move to special createcache OK
 *  Settings - count in raid items OK
 *  Count remaining fir towards nonfir OK
 */

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
