using System.Collections.Generic;

using MoreCheckmarks;

using static AllQuestsCheckmarks.Helpers.MoreCheckmarksBridge;

namespace AllQuestsCheckmarks.MoreCheckmarks.Helpers
{
    internal static class MoreCheckmarksHelper
    {
        private static readonly List<string> traderIds = new List<string>()
        {
            "54cb50c76803fa8b248b4571", //Prapor
            "54cb57776803fa99248b456e", //Therapist
            "579dc571d53a0658a154fbec", //Fence
            "58330581ace78e27b8b10cee", //Skier
            "5935c25fb3acc3127c3d8cd9", //Peacekeeper
            "5a7c2eca46aef81a7ca2145d", //Mechanic
            "5ac3b934156ae10c4430e83c", //Ragman
            "5c0647fdd443bc2504c2d371", //Jaeger
            "638f541a29ffd1183d187f57", //Lightkeeper
            "6617beeaa9cfa777ca915b7c", //Ref
        };

        public static bool GetHideoutUpgrades(string itemId, out int needed, out List<string> areaNames)
        {
            areaNames = new List<string>();
            needed = MoreCheckmarksMod.GetNeeded(itemId, ref areaNames).requiredCount;

            return needed > 0;
        }

        public static bool GetBarters(string itemId, out List<TraderBarters> traders)
        {
            traders = new List<TraderBarters>();
            List<List<KeyValuePair<string, int>>> bartersByTrader = MoreCheckmarksMod.GetBarters(itemId);

            for (int i = 0; i < bartersByTrader.Count; ++i)
            {
                if (bartersByTrader[i] == null || bartersByTrader[i].Count <= 0)
                {
                    continue;
                }

                TraderBarters trader = new TraderBarters(i <= traderIds.Count ? traderIds[i] + " Nickname" : "aqc_custom_trader");
                traders.Add(trader);

                foreach (KeyValuePair<string, int> barter in bartersByTrader[i])
                {
                    trader.Barters.Add(new BarterData(barter.Key + " Name", barter.Value));
                }
            }

            return traders.Count > 0;
        }
    }
}
