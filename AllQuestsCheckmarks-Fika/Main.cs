using AllQuestsCheckmarks.Fika.Helpers;

namespace AllQuestsCheckmarks.Fika
{
    internal static class Main
    {
        public static void Init()
        {
            FikaEventSubscriber.Init();
            Plugin.LogSource.LogInfo("AllQuestsCheckmarks Fika module loaded");
        }
    }
}
