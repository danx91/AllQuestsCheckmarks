using System.Globalization;

using BepInEx.Configuration;
using UnityEngine;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Settings
    {
        public static ConfigEntry<bool> includeCollector;
        public static ConfigEntry<bool> includeLoyaltyRegain;
        public static ConfigEntry<bool> includeNonFir;
        public static ConfigEntry<bool> squadQuests;
        public static ConfigEntry<bool> showDebug;
        public static ConfigEntry<string> checkmarkColor;
        public static ConfigEntry<string> nonFirColor;
        public static ConfigEntry<string> squadColor;

        public static Color _checkmarkColorDefault = new Color(0.749f, 0, 1);
        public static Color _nonFirColorDefault = new Color(0.451f, 0.149f, 0.302f);
        public static Color _squadColorDefault = new Color(1, 0.2f, 0.2f);

        public static Color _checkmarkColor = _checkmarkColorDefault;
        public static Color _nonFirColor = _checkmarkColorDefault;
        public static Color _squadColor = _squadColorDefault;

        public static void Init(ConfigFile config)
        {
            includeCollector = config.Bind("1. General",
                                           "Include Collector quest (Fence)",
                                           true,
                                           "Whether or not to include items needed for Collector quest");

            includeLoyaltyRegain = config.Bind("1. General",
                                               "Include loyalty regain quests",
                                               false,
                                               "Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)");

            includeNonFir = config.Bind("1. General",
                                        "Include non-FiR quest",
                                        true,
                                        "Whether or not to include quests that don't require found in raid items");

            checkmarkColor = config.Bind("2. Colors",
                                         "Checkmark color",
                                         "#bf00ff",
                                         "Color of checkmark if item is not currently needed but is required for future quests (either HEX #ffffff or RGB 255,255,255)");

            nonFirColor = config.Bind("2. Colors",
                                      "Checkmark color (non-FIR)",
                                      "#73264d",
                                      "Color of checkmark if non-FiR item is not currently needed but is required for future quests (either HEX #ffffff or RGB 255,255,255)");

            if (Plugin.isFikaInstalled)
            {
                squadQuests = config.Bind("1. General",
                                          "Mark squad members quests",
                                          true,
                                          "Wether or not to mark items currently needed for players in your squad");

                squadColor = config.Bind("2. Colors",
                                         "Checkmark color (squad members)",
                                         "#ff3333",
                                         "Color of checkmark if item is not currently needed but is required for one of your squad members (either HEX #ffffff or RGB 255,255,255)");
            }

            showDebug = config.Bind("3. Debug",
                                    "Debug logs",
                                    false,
                                    "Log debug info to Player.log");

            config.SettingChanged += SettingChanged;
            ParseColors();

            Plugin.LogSource.LogInfo("Settings loaded");
        }

        private static void SettingChanged(object sender, SettingChangedEventArgs args)
        {
            switch (args.ChangedSetting.Definition.Key)
            {
                case "Checkmark color":
                case "Checkmark color (non-FIR)":
                case "Checkmark color (squad members)":
                    ParseColors();
                    break;
                case "Include Collector quest (Fence)":
                case "Include non-FiR quest":
                case "Include loyalty regain quests":
                    QuestsData.LoadData();
                    break;
            }
        }

        private static void ParseColors()
        {
            _checkmarkColor = ParseColor(checkmarkColor.Value, _checkmarkColorDefault);
            _nonFirColor = ParseColor(nonFirColor.Value, _nonFirColorDefault);

            if (Plugin.isFikaInstalled)
            {
                _squadColor = ParseColor(squadColor.Value, _squadColorDefault);
            }

            Plugin.LogSource.LogInfo($"Colors parsed: checkmarkColor={_checkmarkColor}, nonFirColor={_nonFirColor}, friendColor={_squadColor}");
        }

        private static Color ParseColor(string hexValue, Color defaultColor)
        {
            if (hexValue.IndexOf(",") != -1)
            {
                string[] strNums = hexValue.Split(',');

                if(strNums.Length != 3)
                {
                    Plugin.LogSource.LogWarning("Failed to convert color! (RGB)");
                    return defaultColor;
                }

                float[] nums = new float[3];

                for(int i = 0; i < 3; ++i)
                {
                    if (!int.TryParse(strNums[i], out int result))
                    {
                        Plugin.LogSource.LogWarning("Failed to convert hex color to color! (RGB parse)");
                        return defaultColor;
                    }

                    nums[i] = (result / 255.0f);
                }

                return new Color(nums[0], nums[1], nums[2]);
            }

            if (hexValue.StartsWith("#"))
            {
                hexValue = hexValue.Substring(1);
            }

            if (!int.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int numValue))
            {
                Plugin.LogSource.LogWarning("Failed to convert color! (HEX)");
                return defaultColor;
            }

            return new Color(
                ((numValue >> 16) & 0xFF ) / 255.0f,
                ((numValue >> 8) & 0xFF) / 255.0f,
                (numValue & 0xFF) / 255.0f,
                1);
        }
    }
}
