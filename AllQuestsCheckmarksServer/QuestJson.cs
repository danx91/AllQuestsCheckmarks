using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Serialization;

namespace AllQuestsCheckmarks
{
    internal class QuestJson(Quest quest)
    {
        public QuestStripped Quest { get; } = new QuestStripped(quest);

        public bool? IsUnreachable { get; set; }
    }

    internal class QuestStripped(
        Quest quest,
        List<AvailableForStartCondition>? availableForStartConditions = null,
        List<AvailableForFinishCondition>? availableForFinishCondition = null
    )
    {
        [JsonPropertyName("_id")]
        public MongoId Id { get; init; } = quest.Id;

        public QuestConditions Conditions { get; init; } = new QuestConditions
        {
            AvailableForStart = availableForStartConditions ?? AvailableForStartCondition.FromQuestConditions(quest.Conditions.AvailableForStart),
            AvailableForFinish = availableForFinishCondition ?? AvailableForFinishCondition.FromQuestConditions(quest.Conditions.AvailableForFinish),
        };

        public string? QuestName { get; set; } = quest.QuestName;

        [JsonPropertyName("name")]
        public string LocalizedName { get; init; } = quest.Name;
    }

    internal class QuestConditions
    {
        public List<AvailableForStartCondition>? AvailableForStart { get; set; }

        public List<AvailableForFinishCondition>? AvailableForFinish { get; set; }
    }

    internal class AvailableForStartCondition
    {
        [JsonPropertyName("conditionType")]
        public required string ConditionType { get; set; }

        [JsonPropertyName("target")]
        public string? Target { get; set; }

        public static AvailableForStartCondition FromQuestCondition(QuestCondition condition)
        {
            return new AvailableForStartCondition
            {
                ConditionType = condition.ConditionType,
                Target = condition.Target?.Item,
            };
        }

        public static List<AvailableForStartCondition>? FromQuestConditions(List<QuestCondition>? conditions)
        {
            if (conditions is null)
            {
                return null;
            }

            return conditions.FindAll(c => c.ConditionType is "Quest").ConvertAll(FromQuestCondition);
        }
    }

    internal class AvailableForFinishCondition
    {
        [JsonPropertyName("conditionType")]
        public required string ConditionType { get; set; }

        [JsonPropertyName("target")]
        public List<string>? Target { get; set; }

        [JsonPropertyName("onlyFoundInRaid")]
        public bool? OnlyFoundInRaid { get; set; }

        [JsonPropertyName("value")]
        public double? Value { get; set; }

        public static AvailableForFinishCondition FromQuestCondition(QuestCondition condition)
        {
            return new AvailableForFinishCondition
            {
                ConditionType = condition.ConditionType,
                Target = condition.Target?.List,
                OnlyFoundInRaid = condition.OnlyFoundInRaid,
                Value = condition.Value
            };
        }

        public static List<AvailableForFinishCondition>? FromQuestConditions(List<QuestCondition>? conditions)
        {
            if (conditions is null)
            {
                return null;
            }

            return conditions.FindAll(c => c.ConditionType is
                "LeaveItemAtLocation" or
                "HandoverItem" or
                "FindItem"
            ).ConvertAll(FromQuestCondition);
        }
    }
}
