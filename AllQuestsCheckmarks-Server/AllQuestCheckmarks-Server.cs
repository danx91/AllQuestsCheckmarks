using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using Range = SemanticVersioning.Range;

namespace EchoesOfTarkovRequisitions;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.crypluto.echoesoftarkov.requisitions";
    public override string Name { get; init; } = "Echoes Of Tarkov Requisitions";
    public override string Author { get; init; } = "Pluto";
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.3");
    public override Range SptVersion { get; init; } = new("~4.0.0");
    public override string License { get; init; } = "MIT";
    public override bool? IsBundleMod { get; init; } = true;
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.0") }
    };
    public override string? Url { get; init; }
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class EchoesOfTarkovRequisitions(
    ISptLogger<EchoesOfTarkovRequisitions> logger,
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    DatabaseService databaseService
) : IOnLoad
{