using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Locales
    {
        private static readonly List<string> _loadedLocales = new List<string>();

        public static void LoadLocale(string localeId)
        {
            if (_loadedLocales.Contains(localeId) || !LocaleManagerClass.LocaleManagerClass.ContainsCulture(localeId))
            {
                return;
            }

            Plugin.LogSource.LogInfo($"Loding locale: {localeId}");

            string path = $"{Plugin.modPath}/locales/{localeId}.json";

            if (!File.Exists(path))
            {
                Plugin.LogSource.LogWarning($"Couldn't load locale {localeId} - file does not exist! Falling back to en.json");
                path = $"{Plugin.modPath}/locales/en.json";
            }

            JObject localeJSON = JObject.Parse(File.ReadAllText(path));
            Dictionary<string, string> localeDict = new Dictionary<string, string>();

            foreach (KeyValuePair<string, JToken> item in localeJSON)
            {
                localeDict.Add(item.Key, item.Value.ToString());
            }

            if (localeDict.Count <= 0)
            {
                Plugin.LogSource.LogWarning($"Local {localeId} is empty!");
                return;
            }

            _loadedLocales.Add(localeId);
            LocaleManagerClass.LocaleManagerClass.UpdateLocales(localeId, localeDict);
        }
    }
}
