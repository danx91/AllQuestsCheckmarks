using BepInEx.Configuration;
using UnityEngine;
using ZGFueDkx.ZGCLib.Config;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class Settings
    {
        public static ConfigEntry<bool>? IncludeCollector;
        public static ConfigEntry<bool>? IncludeLoyaltyRegain;
        public static ConfigEntry<bool>? IncludeUnreachable;
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

        public static ConfigEntry<Color>? CheckmarkColor;
        public static ConfigEntry<Color>? NonFirColor;
        public static ConfigEntry<Color>? CollectorColor;
        public static ConfigEntry<Color>? EnoughItemsColor;
        public static ConfigEntry<Color>? CustomQuestColor;
        public static ConfigEntry<Color>? SquadColor;
        public static ConfigEntry<Color>? ActiveQuestTextColor;
        public static ConfigEntry<Color>? FutureQuestTextColor;
        public static ConfigEntry<Color>? SquadQuestTextColor;

        public static void Init(ConfigFile config)
        {
            ConfigCategory general = config.MakeCategory(1, "General");
            ConfigCategory colors = config.MakeCategory(2, "Colors");
            ConfigCategory text = config.MakeCategory(3, "Text");
            ConfigCategory debug = config.MakeCategory(9, "Debug");

            /*
             * GENERAL
             */
            IncludeCollector = general.BindConfig(
                "Include Collector quest (Fence)",
                true,
                "Whether or not to include items needed for Collector quest"
            );

            IncludeNonFir = general.BindConfig(
                "Include non-FiR quests",
                true,
                "Whether or not to include quests that don't require found in raid items"
            );

            IncludeLoyaltyRegain = general.BindConfig(
                "Include loyalty regain quests",
                false,
                "Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)"
            );

            IncludeUnreachable = general.BindConfig(
                "Include unreachable quests",
                false,
                "Whether or not to include quests that are unreachable (event quests and quests for other account types)"
            );

            HideFulfilled = general.BindConfig(
                "Hide checkmark if have enough (in raid)",
                false,
                "Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with " +
                    "'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!"
            );

            OnlyActiveQuests = general.BindConfig(
                "Show only active quests",
                false,
                "Whether or not to show only active quests (no future quests)"
            );

            IncludeRaidItems = general.BindConfig(
                "Include items in PMC inventory (in raid)",
                false,
                "Whether or not to include items in PMC inventory while in raid in 'In Stash' count"
            );

            /*
             * COLORS
             */
            CheckmarkColor = colors.BindColor(
                "Checkmark color",
                "#bf00ff",
                "Color of checkmark if item is not currently needed but is required for future quests"
            );

            NonFirColor = colors.BindColor(
                "Checkmark color (non-FIR)",
                "#73264d",
                "Color of checkmark if non-FiR item is not currently needed but is required for future quests"
            );

            CollectorColor = colors.BindColor(
                "Collector color",
                "#bf00ff",
                "Color of checkmark for collector quest"
            );

            MarkEnoughItems = colors.BindConfig(
                "Use different color if have enough",
                false,
                "Whether or not to use different checkmark color if you have enough items for all quests. " +
                    "'Hide checkmark if have enough' option will hide this checkmark while in raid"
            );

            EnoughItemsColor = colors.BindColor(
                "Have enough color",
                "#00ff00",
                "Color of checkmark if you have enough items for all quests"
            );

            UseCustomQuestColor = colors.BindConfig(
                "Use custom quest checkmark color",
                false,
                "Whether or not to use custom checkmark color for active quests"
            );

            CustomQuestColor = colors.BindColor(
                "Custom quest color",
                "#ffeb6d",
                "Custom color of default quest checkmark"
            );

            /*
             * TEXT
             */
            BulletPoints = text.BindConfig(
                "Use bullet points",
                true,
                "Whether or not to use bullet points in quests list"
            );

            CustomTextColors = text.BindConfig(
                "Use custom text colors",
                false,
                "Whether or not to use custom text colors"
            );

            ActiveQuestTextColor = text.BindColor(
                "Custom text color - active quests",
                "#dd831a",
                "Custom color of active quests text"
            );

            FutureQuestTextColor = text.BindColor(
                "Custom text color - future quests",
                "#d24dff",
                "Custom color of future quests text"
            );

            if (Plugin.isFikaInstalled)
            {
                SquadQuests = general.BindConfig(
                    "Mark squad members quests",
                    true,
                    "Wether or not to mark items currently needed for players in your squad"
                );

                SquadColor = colors.BindColor(
                    "Checkmark color (squad members)",
                    "#ff3333",
                    "Color of checkmark if item is not currently needed but is required for one of your squad members"
                );

                SquadQuestTextColor = text.BindColor(
                    "Custom text color - squad quests",
                    "#ffc299",
                    "Custom color of squad quests text"
                );
            }

            /*
             * DEBUG
             */
            ShowDebug = debug.BindConfig(
                "Debug logs",
                false,
                "Enable debug logs in Player.log"
            );

            debug.BindButton(
                "Reload quests data",
                "Reload",
                "Reload quests data from server",
                () =>
                {
                    QuestsData.LoadData();
                }
            );

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
    }
}
