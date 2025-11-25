using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using Range = SemanticVersioning.Range;

namespace AllQuestCheckmarks_Server;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "ZGFueDkx.AllQuestCheckmarks";
    public override string Name { get; init; } = "All Quest Checkmarks Server";
    public override string Author { get; init; } = "Pluto";
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.0");
    public override string License { get; init; } = "MIT";
    public override bool? IsBundleMod { get; init; } = true;
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 9000)]
public class AllQuestCheckmarksServer(
    ISptLogger<AllQuestCheckmarksServer> logger,
    QuestHelper questHelper,
    ConfigServer configServer,
    ProfileHelper profileHelper,
    CustomStaticRouter customStaticRouter) : IOnLoad
{
    public required QuestHelper QuestHelper;
    public required ConfigServer ConfigServer;
    public required ProfileHelper ProfileHelper;
    
    public Task OnLoad()
    {
        QuestHelper = questHelper;
        ConfigServer = configServer;
        ProfileHelper = profileHelper;
        
        customStaticRouter.PassData(QuestHelper, ConfigServer, ProfileHelper, logger);
        
        logger.Success("[AllQuestCheckmarksServer] Mod loaded successfully.");
        
        return Task.CompletedTask;
    }
}