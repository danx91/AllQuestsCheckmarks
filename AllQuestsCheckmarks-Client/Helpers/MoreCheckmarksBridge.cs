using System;
using System.Collections.Generic;
using System.Reflection;

namespace AllQuestsCheckmarks.Helpers
{
    public static class MoreCheckmarksBridge
    {
        public class BarterData
        {
            public string BarterName;
            public int Count;

            public BarterData(string barterName, int count)
            {
                BarterName = barterName;
                Count = count;
            }
        }

        public class TraderBarters
        {
            public string TraderName;
            public List<BarterData> Barters = new List<BarterData>();

            public TraderBarters(string traderName)
            {
                TraderName = traderName;
            }
        }

        public delegate bool HideoutAreasDelegate(string itemId, out int needed, out List<string> areaNames);
        public delegate bool BarersDelegate(string itemId, out List<TraderBarters> traders);

        public static event HideoutAreasDelegate GetHideoutAreasEvent;
        public static event BarersDelegate GetBartersEvent;

        public static void Init()
        {
            TryInitMoreCheckmarks();
        }

        public static bool InvokeGetHideoutAreasEvent(string itemId, out int needed, out List<string> areaNames)
        {
            return GetHideoutAreasEvent.Invoke(itemId, out needed, out areaNames);
        }

        public static bool InvokeGetBartersEvent(string itemId, out List<TraderBarters> traders)
        {
            return GetBartersEvent.Invoke(itemId, out traders);
        }

        private static bool TryInitMoreCheckmarks()
        {
            try
            {
                Assembly assembly = Assembly.Load("AllQuestsCheckmarks-MoreCheckmarks");
                Type main = assembly.GetType("AllQuestsCheckmarks.MoreCheckmarks.Main");
                MethodInfo init = main.GetMethod("Init");

                init.Invoke(main, null);
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError($"Failed to load AllQuestsCheckmarks-MoreCheckmarks.dll! : {e}");
                return false;
            }

            return true;
        }
    }
}
