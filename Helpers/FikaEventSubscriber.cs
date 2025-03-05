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
                Plugin.LogDebug("FIKA Raid Started");

                if(!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    Plugin.LogSource.LogError("Failed to get FIKA CoopHandler");
                    return;
                }

                List<CoopPlayer> players = new List<CoopPlayer>();

                foreach(CoopPlayer player in coopHandler.HumanPlayers)
                {
                    Plugin.LogDebug($"Player {player.Profile.Nickname} : {player.ProfileId}");
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
                Plugin.LogDebug("FIKA Game Ended");
                SquadQuests.ClearSquadQuests();
            });
        }
    }
}
