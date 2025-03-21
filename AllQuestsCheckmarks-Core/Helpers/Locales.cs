using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Locales
    {
        private static readonly List<string> _loadedLocales = new List<string>();
        private static Dictionary<string, string> _english;

        public static void LoadLocale(string localeId)
        {
            if (_loadedLocales.Contains(localeId) || !LocaleManagerClass.LocaleManagerClass.ContainsCulture(localeId))
            {
                return;
            }

            if(_english == null)
            {
                _english = ReadLocale($"{Plugin.modPath}/locales/en.json");
            }

            Plugin.LogSource.LogInfo($"Loding locale: {localeId}");

            Dictionary<string, string> localeDict = null;
            string path = $"{Plugin.modPath}/locales/{localeId}.json";

            if (File.Exists(path))
            {
                localeDict = ReadLocale(path);

                if (localeDict.Count <= 0)
                {
                    Plugin.LogSource.LogWarning($"Local {localeId} is empty!");
                    localeDict = new Dictionary<string, string>(_english);
                }
                else if (localeId != "en")
                {
                    foreach(KeyValuePair<string, string> englishEntry in _english)
                    {
                        if (!localeDict.ContainsKey(englishEntry.Key))
                        {
                            Plugin.LogSource.LogWarning($"Locale '{localeId}' is missing entry '{englishEntry.Key}'");
                            localeDict.Add(englishEntry.Key, englishEntry.Value);
                        }
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"Couldn't load locale '{localeId}' - file does not exist! Falling back to en.json");
                localeDict = new Dictionary<string, string>(_english);
            }

            _loadedLocales.Add(localeId);
            LocaleManagerClass.LocaleManagerClass.UpdateLocales(localeId, localeDict);
        }

        private static Dictionary<string, string> ReadLocale(string path)
        {
            JObject localeJSON = JObject.Parse(File.ReadAllText(path));
            Dictionary<string, string> localeDict = new Dictionary<string, string>();

            foreach (KeyValuePair<string, JToken> item in localeJSON)
            {
                localeDict.Add(item.Key, item.Value.ToString());
            }

            return localeDict;
        }
    }
}
