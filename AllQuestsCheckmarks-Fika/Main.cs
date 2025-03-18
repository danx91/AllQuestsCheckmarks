using AllQuestsCheckmarks.Fika.Helpers;

namespace AllQuestsCheckmarks.Fika
{
    internal class Main
    {
        public static void Init()
        {
            FikaEventSubscriber.Init();
            Plugin.LogSource.LogInfo("AllQuestsCheckmarks Fika module loaded");
        }
    }
}
