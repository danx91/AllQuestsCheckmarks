using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Settings
    {
        private static readonly Color _fallbackColor = new Color(0, 0, 0, 0);
        private static readonly List<ColorEntry> _allColors = new List<ColorEntry>();

        public static ConfigEntry<bool>? IncludeCollector;
        public static ConfigEntry<bool>? IncludeLoyaltyRegain;
        public static ConfigEntry<bool>? IncludeNonFir;
        public static ConfigEntry<bool>? HideFulfilled;
        public static ConfigEntry<bool>? OnlyActiveQuests;
        public static ConfigEntry<bool>? IncludeRaidItems;
        public static ConfigEntry<bool>? SquadQuests;
        public static ConfigEntry<bool>? MarkEnoughItems;
        public static ConfigEntry<bool>? UseCustomQuestColor;
        public static ConfigEntry<bool>? BulletPoints;
        public static ConfigEntry<bool>? CustomTextColors;
        public static ConfigEntry<bool>? ShowDebug;

        public static ColorEntry? CheckmarkColor;
        public static ColorEntry? NonFirColor;
        public static ColorEntry? CollectorColor;
        public static ColorEntry? EnoughItemsColor;
        public static ColorEntry? CustomQuestColor;
        public static ColorEntry? SquadColor;
        public static ColorEntry? ActiveQuestTextColor;
        public static ColorEntry? FutureQuestTextColor;
        public static ColorEntry? SquadQuestTextColor;

        public class ColorEntry
        {
            public ConfigEntry<string> Entry;
            public Color Color;
            public string Hex;

            private Color _color;

            public ColorEntry(ConfigEntry<string> entry, Color color)
            {
                this.Entry = entry;
                this._color = color;
                this.Color = color;
                this.Hex = ColorToHex(color);

                entry.SettingChanged += delegate
                {
                    Parse();
                };

                _allColors.Add(this);
            }

            public void Parse()
            {
                this.Color = ParseColor(Entry.Value, _color);
                this.Hex = ColorToHex(Color);

                Plugin.LogDebug($"Color parsed: {Entry.Definition.Key}, color: {this.Color}, hex: {this.Hex}");
            }
        }

        public static void Init(ConfigFile config)
        {
            int generalIndex = 99;
            int colorsIndex = 199;
            int textIndex = 299;

            /*
             * GENERAL
             */
            IncludeCollector = config.Bind(
                "1. General",
                "Include Collector quest (Fence)",
                true,
                MakeDescription(
                    "Whether or not to include items needed for Collector quest",
                    generalIndex--
                )
            );

            IncludeNonFir = config.Bind(
                "1. General",
                "Include non-FiR quest",
                true,
                MakeDescription(
                    "Whether or not to include quests that don't require found in raid items",
                    generalIndex--
                )
            );

            IncludeLoyaltyRegain = config.Bind(
                "1. General",
                "Include loyalty regain quests",
                false,
                MakeDescription(
                    "Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)",
                    generalIndex--
                )
            );

            HideFulfilled = config.Bind(
                "1. General",
                "Hide checkmark if have enough (in raid)",
                false,
                MakeDescription(
                    "Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with " +
                        "'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!",
                    generalIndex--
                )
            );

            OnlyActiveQuests = config.Bind(
                "1. General",
                "Show only active quests",
                false,
                MakeDescription(
                    "Whether or not to show only active quests (no future quests)",
                    generalIndex--
                )
            );

            IncludeRaidItems = config.Bind(
                "1. General",
                "Include items in PMC inventory (in raid)",
                false,
                MakeDescription(
                    "Whether or not to include items in PMC inventory while in raid in 'In Stash' count",
                    generalIndex--
                )
            );

            /*
             * COLORS
             */
            CheckmarkColor = config.BindColor(
                "2. Colors",
                "Checkmark color",
                "#bf00ff",
                "Color of checkmark if item is not currently needed but is required for future quests",
                colorsIndex--
            );

            NonFirColor = config.BindColor(
                "2. Colors",
                "Checkmark color (non-FIR)",
                "#73264d",
                "Color of checkmark if non-FiR item is not currently needed but is required for future quests",
                colorsIndex--
            );

            CollectorColor = config.BindColor(
                "2. Colors",
                "Collector color",
                "#bf00ff",
                "Color of checkmark for collector quest",
                colorsIndex--
            );

            MarkEnoughItems = config.Bind(
                "2. Colors",
                "Use different color if have enough",
                false,
                MakeDescription(
                    "Whether or not to use different checkmark color if you have enough items for all quests. " +
                        "'Hide checkmark if have enough' option will hide this checkmark while in raid",
                    generalIndex--
                )
            );

            EnoughItemsColor = config.BindColor(
                "2. Colors",
                "Have enough color",
                "#00ff00",
                "Color of checkmark if you have enough items for all quests",
                colorsIndex--
            );

            UseCustomQuestColor = config.Bind(
                "2. Colors",
                "Use custom quest checkmark color",
                false,
                MakeDescription(
                    "Whether or not to use custom checkmark color for active quests",
                    generalIndex--
                )
            );

            CustomQuestColor = config.BindColor(
                "2. Colors",
                "Custom quest color",
                "#ffeb6d",
                "Custom color of default quest checkmark",
                colorsIndex--
            );

            /*
             * TEXT
             */
            BulletPoints = config.Bind(
                "3. Text",
                "Use bullet points",
                true,
                MakeDescription(
                    "Whether or not to use bullet points in quests list",
                    textIndex--
                )
            );

            CustomTextColors = config.Bind(
                "3. Text",
                "Use custom text colors",
                false,
                MakeDescription(
                    "Whether or not to use custom text colors",
                    textIndex--
                )
            );

            ActiveQuestTextColor = config.BindColor(
                "3. Text",
                "Custom text color - active quests",
                "#dd831a",
                "Custom color of active quests text",
                textIndex--
            );

            FutureQuestTextColor = config.BindColor(
                "3. Text",
                "Custom text color - future quests",
                "#d24dff",
                "Custom color of future quests text",
                textIndex--
            );

            if (Plugin.isFikaInstalled)
            {
                SquadQuests = config.Bind(
                    "1. General",
                    "Mark squad members quests",
                    true,
                    MakeDescription(
                        "Wether or not to mark items currently needed for players in your squad",
                        generalIndex--
                    )
                );

                SquadColor = config.BindColor(
                    "2. Colors",
                    "Checkmark color (squad members)",
                    "#ff3333",
                    "Color of checkmark if item is not currently needed but is required for one of your squad members",
                    colorsIndex--
                );

                SquadQuestTextColor = config.BindColor(
                    "3. Text",
                    "Custom text color - squad quests",
                    "#ffc299",
                    "Custom color of squad quests text",
                    textIndex--
                );
            }

            /*
             * DEBUG
             */
            ShowDebug = config.Bind(
                "9. Debug",
                "Debug logs",
                false,
                "Log debug info to Player.log"
            );

            foreach (ColorEntry color in _allColors)
            {
                color.Parse();
            }

            config.SettingChanged += SettingChanged;
            Plugin.LogSource?.LogInfo("Settings loaded");
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
                    Plugin.LogSource?.LogWarning("Failed to convert color! (RGB)");
                    return defaultColor;
                }

                float[] nums = new float[3];

                for(int i = 0; i < 3; ++i)
                {
                    if (!int.TryParse(strNums[i], out int result))
                    {
                        Plugin.LogSource?.LogWarning("Failed to convert hex color to color! (RGB parse)");
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
                Plugin.LogSource?.LogWarning("Failed to convert color! (HEX)");
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
