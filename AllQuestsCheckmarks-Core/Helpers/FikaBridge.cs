using System;
using System.Collections.Generic;

namespace AllQuestsCheckmarks.Helpers
{
    public static class FikaBridge
    {
        public static event Action RaidFinishedEvent;
        public static event Action BuildCacheEvent;
        public static event Action<Dictionary<string, string>> CoopPlayersEvent;

        public static void Init()
        {
            RaidFinishedEvent += SquadQuests.ClearSquadQuests;
            BuildCacheEvent += QuestsHelper.BuildItemsCache;
            CoopPlayersEvent += SquadQuests.LoadData;
        }

        public static void InvokeRaidFinishedEvent()
        {
            RaidFinishedEvent.Invoke();
        }

        public static void InvokeCoopPlayersEvent(Dictionary<string, string> players)
        {
            CoopPlayersEvent.Invoke(players);
        }

        public static void InvokeBuildCacheEvent()
        {
            BuildCacheEvent.Invoke();
        }
    }
}
