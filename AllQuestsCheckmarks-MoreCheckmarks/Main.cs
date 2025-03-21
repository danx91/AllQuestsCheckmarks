using AllQuestsCheckmarks.Helpers;
using AllQuestsCheckmarks.MoreCheckmarks.Helpers;

namespace AllQuestsCheckmarks.MoreCheckmarks
{
    public static class Main
    {
        public static void Init()
        {
            MoreCheckmarksBridge.GetHideoutAreasEvent += MoreCheckmarksHelper.GetHideoutUpgrades;
            MoreCheckmarksBridge.GetBartersEvent += MoreCheckmarksHelper.GetBarters;

            Plugin.LogSource.LogInfo("AllQuestsCheckmarks MoreCheckmarks module loaded");
        }
    }
}
