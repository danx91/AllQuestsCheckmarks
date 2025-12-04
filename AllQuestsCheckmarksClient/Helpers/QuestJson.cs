using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AllQuestsCheckmarks.Helpers
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class JsonIgnoreError : Attribute;

    internal static class JsonSettingsProvider
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            Error = HandleError,
        };

        private static void HandleError(object sender, ErrorEventArgs args)
        {
            Plugin.LogDebug($"JSON Error at {args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");

            args.ErrorContext.Handled = true;

            var currentObject = args.CurrentObject;
            if (currentObject == null)
            {
                return;
            }

            PropertyInfo? property = FindProperty(currentObject.GetType(), args.ErrorContext.Path);
            if (property != null)
            {
                JsonIgnoreError? ignoreAttr = property.GetCustomAttribute<JsonIgnoreError>(true);
                if (ignoreAttr != null)
                {
                    return;
                }
            }

            Plugin.LogSource?.LogError($"JSON Error at {args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
        }

        private static PropertyInfo? FindProperty(Type type, string path)
        {
            string raw = path.Split('.').Last();
            string clean = raw.Split('[', ']')[0];

            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(prop =>
                {
                    var attr = prop.GetCustomAttribute<JsonPropertyAttribute>();
                    return attr?.PropertyName == clean || prop.Name == clean;
                });
        }
    }

    internal class QuestJson
    {
        public Quest? Quest { get; set; }

        public bool? IsUnreachable { get; set; }
    }

    internal class Quest
    {
        [JsonProperty("_id")]
        public MongoID? Id { get; set; }

        public QuestConditions? Conditions { get; set; }

        public string? QuestName { get; set; }

        [JsonProperty("name")]
        public string? LocalizedName { get; set; }
    }

    internal class QuestConditions
    {
        public List<AvailableForStartCondition>? AvailableForStart { get; set; }

        public List<AvailableForFinishCondition>? AvailableForFinish { get; set; }
    }

    internal class AvailableForStartCondition
    {
        [JsonProperty("conditionType")]
        public string? ConditionType { get; set; }

        [JsonProperty("target"), JsonIgnoreError]
        public string? Target { get; set; }
    }

    internal class AvailableForFinishCondition
    {
        [JsonProperty("conditionType")]
        public string? ConditionType { get; set; }

        [JsonProperty("target"), JsonIgnoreError]
        public List<string>? Target { get; set; }

        [JsonProperty("onlyFoundInRaid")]
        public bool? OnlyFoundInRaid { get; set; }

        [JsonProperty("value")]
        public int? Value { get; set; }
    }
}
