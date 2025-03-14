using System.Collections.Generic;
using System.Globalization;

using BepInEx.Configuration;
using UnityEngine;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Settings
    {
        private static readonly Color _fallbackColor = new Color(0, 0, 0, 0);
        private static readonly List<ColorEntry> allColors = new List<ColorEntry>();

        public static ConfigEntry<bool> includeCollector;
        public static ConfigEntry<bool> includeLoyaltyRegain;
        public static ConfigEntry<bool> includeNonFir;
        public static ConfigEntry<bool> hideFulfilled;
        public static ConfigEntry<bool> includeRaidItems;
        public static ConfigEntry<bool> squadQuests;
        public static ConfigEntry<bool> markEnoughItems;
        public static ConfigEntry<bool> useCustomQuestColor;
        public static ConfigEntry<bool> bulletPoint;
        public static ConfigEntry<bool> customTextColors;
        public static ConfigEntry<bool> showDebug;

        public static ColorEntry checkmarkColor;
        public static ColorEntry nonFirColor;
        public static ColorEntry collectorColor;
        public static ColorEntry enoughItemsColor;
        public static ColorEntry customQuestColor;
        public static ColorEntry squadColor;
        public static ColorEntry activeQuestTextColor;
        public static ColorEntry futureQuestTextColor;
        public static ColorEntry squadQuestTextColor;

        public class ColorEntry
        {
            public ConfigEntry<string> entry;
            public Color color;
            public string hex;

            private Color _color;

            public ColorEntry(ConfigEntry<string> entry, Color color)
            {
                this.entry = entry;
                this._color = color;
                this.color = color;
                this.hex = ColorToHex(color);

                entry.SettingChanged += delegate
                {
                    Parse();
                };

                allColors.Add(this);
            }

            public void Parse()
            {
                this.color = ParseColor(entry.Value, _color);
                this.hex = ColorToHex(color);

                Plugin.LogDebug($"Color parsed: {entry.Definition.Key}, color: {this.color}, hex: {this.hex}");
            }
        }

        public static void Init(ConfigFile config)
        {
            includeCollector = config.Bind(
                "1. General",
                "Include Collector quest (Fence)",
                true,
                MakeDescription(
                    "Whether or not to include items needed for Collector quest",
                    99
                )
            );

            includeNonFir = config.Bind(
                "1. General",
                "Include non-FiR quest",
                true,
                MakeDescription(
                    "Whether or not to include quests that don't require found in raid items",
                    98
                )
            );

            includeLoyaltyRegain = config.Bind(
                "1. General",
                "Include loyalty regain quests",
                false,
                MakeDescription(
                    "Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)",
                    97
                )
            );

            hideFulfilled = config.Bind(
                "1. General",
                "Hide checkmark if have enough (in raid)",
                false,
                MakeDescription(
                    "Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with " +
                        "'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!",
                    96
                )
            );

            includeRaidItems = config.Bind(
                "1. General",
                "Include items in PMC inventory (in raid)",
                false,
                MakeDescription(
                    "Whether or not to include items in PMC inventory while in raid in 'In Stash' count",
                    95
                )
            );

            checkmarkColor = config.BindColor(
                "2. Colors",
                "Checkmark color",
                "#bf00ff",
                "Color of checkmark if item is not currently needed but is required for future quests",
                199
            );

            nonFirColor = config.BindColor(
                "2. Colors",
                "Checkmark color (non-FIR)",
                "#73264d",
                "Color of checkmark if non-FiR item is not currently needed but is required for future quests",
                198
            );

            collectorColor = config.BindColor(
                "2. Colors",
                "Collector color",
                "#bf00ff",
                "Color of checkmark for collector quest",
                197
            );

            markEnoughItems = config.Bind(
                "2. Colors",
                "Use different color if have enough",
                false,
                MakeDescription(
                    "Whether or not to use different checkmark color if you have enough items for all quests. " +
                        "'Hide checkmark if have enough' option will hide this checkmark while in raid",
                    195
                )
            );

            enoughItemsColor = config.BindColor(
                "2. Colors",
                "Have enough color",
                "#00ff00",
                "Color of checkmark if you have enough items for all quests",
                194
            );

            useCustomQuestColor = config.Bind(
                "2. Colors",
                "Use custom quest checkmark color",
                false,
                MakeDescription(
                    "Whether or not to use custom checkmark color for active quests",
                    193
                )
            );

            customQuestColor = config.BindColor(
                "2. Colors",
                "Custom quest color",
                "#ffeb6d",
                "Custom color of default quest checkmark",
                192
            );

            bulletPoint = config.Bind(
                "3. Text",
                "Use bullet points",
                true,
                MakeDescription(
                    "Whether or not to use bullet points in quests list",
                    299
                )
            );

            customTextColors = config.Bind(
                "3. Text",
                "Use custom text colors",
                false,
                MakeDescription(
                    "Whether or not to use custom text colors",
                    298
                )
            );

            activeQuestTextColor = config.BindColor(
                "3. Text",
                "Custom text color - active quests",
                "#dd831a",
                "Custom color of active quests text",
                297
            );

            futureQuestTextColor = config.BindColor(
                "3. Text",
                "Custom text color - future quests",
                "#d24dff",
                "Custom color of future quests text",
                296
            );

            if (Plugin.isFikaInstalled)
            {
                squadQuests = config.Bind(
                    "1. General",
                    "Mark squad members quests",
                    true,
                    MakeDescription(
                        "Wether or not to mark items currently needed for players in your squad",
                        94
                    )
                );

                squadColor = config.BindColor(
                    "2. Colors",
                    "Checkmark color (squad members)",
                    "#ff3333",
                    "Color of checkmark if item is not currently needed but is required for one of your squad members",
                    196
                );

                squadQuestTextColor = config.BindColor(
                    "3. Text",
                    "Custom text color - squad quests",
                    "#ffc299",
                    "Custom color of squad quests text",
                    295
                );
            }

            showDebug = config.Bind(
                "9. Debug",
                "Debug logs",
                false,
                "Log debug info to Player.log"
            );

            foreach(ColorEntry color in allColors)
            {
                color.Parse();
            }

            config.SettingChanged += SettingChanged;
            Plugin.LogSource.LogInfo("Settings loaded");
        }

        private static void SettingChanged(object sender, SettingChangedEventArgs args)
        {
            switch (args.ChangedSetting.Definition.Key)
            {
                case "Include Collector quest (Fence)":
                case "Include non-FiR quest":
                case "Include loyalty regain quests":
                    QuestsData.LoadData();
                    break;
            }
        }

        private static ConfigDescription MakeDescription(string description, int order)
        {
            return new ConfigDescription(
                description,
                null,
                new ConfigurationManagerAttributes
                {
                    IsAdvanced = false,
                    Order = order,
                }
            );
        }

        private static ColorEntry BindColor(this ConfigFile config, string section, string key, string color, string description, int order)
        {
            return new ColorEntry(
                config.Bind(section, key, color, MakeDescription(description + " (either HEX #ffffff or RGB 255,255,255)", order)),
                ParseColor(color, _fallbackColor)
            );
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

        private static string ColorToHex(Color color)
        {
            return "#"
                + ((int)(color.r * 255)).ToString("X").PadLeft(2, '0')
                + ((int)(color.g * 255)).ToString("X").PadLeft(2, '0')
                + ((int)(color.b * 255)).ToString("X").PadLeft(2, '0');
        }
    }
}
