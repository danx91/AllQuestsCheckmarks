using System.Collections.Generic;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using AllQuestsCheckmarks.Helpers;

namespace AllQuestsCheckmarks.Fika.Helpers
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

                Dictionary<string, string> players = new Dictionary<string, string>();

                foreach(FikaPlayer player in coopHandler.HumanPlayers)
                {
                    if(player != coopHandler.MyPlayer)
                        players.Add(player.ProfileId, player.Profile.Nickname);
                }

                if(players.Count == 0)
                    return;

                FikaBridge.InvokeCoopPlayersEvent(players);
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameEndedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Ended");
                FikaBridge.InvokeRaidFinishedEvent();
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameCreatedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Created");
                FikaBridge.InvokeBuildCacheEvent();
            });
        }
    }
}
