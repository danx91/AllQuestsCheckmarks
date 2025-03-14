using System.Collections.Generic;

using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class FikaEventSubscriber
    {
        public static void Init()
        {
            FikaEventDispatcher.SubscribeEvent((FikaRaidStartedEvent e) =>
            {
                Plugin.LogDebug("Fika Raid Started");

                if(!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    Plugin.LogSource.LogError("Failed to get Fika CoopHandler");
                    return;
                }

                List<CoopPlayer> players = new List<CoopPlayer>();

                foreach(CoopPlayer player in coopHandler.HumanPlayers)
                {
                    if(player != coopHandler.MyPlayer)
                    {
                        players.Add(player);
                    }
                }

                if(players.Count == 0)
                {
                    return;
                }

                SquadQuests.LoadData(players);
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameEndedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Ended");
                SquadQuests.ClearSquadQuests();
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameCreatedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Created");
                QuestsHelper.BuildItemsCache();
            });
        }
    }
}
