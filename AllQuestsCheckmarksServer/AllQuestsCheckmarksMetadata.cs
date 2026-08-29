using SPTarkov.Server.Core.Models.Spt.Mod;

namespace AllQuestsCheckmarks
{
    public record SkillDistributionMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.zgfuedkx.allquestscheckmarks";
        public string Name { get; init; } = "All Quests Checkmarks";
        public string Author { get; init; } = "ZGFueDkx";
        public List<string>? Contributors { get; init; }
        public SemanticVersioning.Version Version { get; init; } = new("1.4.0");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; } = "https://github.com/danx91/AllQuestsCheckmarks";
        public string License { get; init; } = "GNU GPLv3";
        public bool HasPrepatcher { get; init; } = false;
    }
}
