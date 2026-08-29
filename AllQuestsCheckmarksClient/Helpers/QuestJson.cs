using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using ZGFueDkx.ZGCLib.Helpers;

namespace AllQuestsCheckmarks.Helpers
{
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

        [JsonProperty("target"), JsonUtils.JsonIgnoreError]
        public string? Target { get; set; }
    }

    internal class AvailableForFinishCondition
    {
        [JsonProperty("conditionType")]
        public string? ConditionType { get; set; }

        [JsonProperty("target"), JsonUtils.JsonIgnoreError]
        public List<string>? Target { get; set; }

        [JsonProperty("onlyFoundInRaid")]
        public bool? OnlyFoundInRaid { get; set; }

        [JsonProperty("value")]
        public int? Value { get; set; }
    }
}
