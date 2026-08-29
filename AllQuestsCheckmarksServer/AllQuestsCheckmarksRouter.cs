using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace AllQuestsCheckmarks
{
    [Injectable(TypePriority = OnLoadOrder.Routers + 1)]
    internal class AllQuestsCheckmarksRouter(
        JsonUtil jsonUtil,
        AllQuestsCheckmarksMod allQuestsCheckmarksMod
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/all-quests-checkmarks/quests",
                (url, info, sessionId, output, cancellationToken) => allQuestsCheckmarksMod.GetAllQuests(sessionId)
            ),
            new RouteAction<ActiveQuestsRequestData>(
                "/all-quests-checkmarks/active-quests",
                (url, info, sessionId, output, cancellationToken) => allQuestsCheckmarksMod.HandleGetActiveQuests(info)
            ),
        ]
    )
    {
        private class ActiveQuestsRequestData : List<MongoId>, IRequestData;
    }
}
