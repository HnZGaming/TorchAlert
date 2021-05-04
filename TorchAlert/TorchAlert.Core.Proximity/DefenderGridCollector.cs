using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;

namespace TorchAlert.Core.Proximity
{
    public sealed class DefenderGridCollector
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly AlertableSteamIdExtractor _steamIdExtractor;

        public DefenderGridCollector(AlertableSteamIdExtractor steamIdExtractor)
        {
            _steamIdExtractor = steamIdExtractor;
        }

        public IEnumerable<DefenderGridInfo> CollectDefenderGrids()
        {
            _steamIdExtractor.Update();

            var groups = MyCubeGridGroups.Static.Logical.Groups;
            foreach (var group in groups)
            {
                var grid = group.GroupData.Root;
                Log.Trace($"\"{grid.DisplayName}\"");

                if (!grid.IsStatic) continue;
                Log.Trace("grid is static");

                var gridOwnerIds = grid.BigOwners;
                if (!gridOwnerIds.TryGetFirst(out var ownerId)) continue;
                Log.Trace("grid has owner");

                var steamIds = _steamIdExtractor.GetAlertableSteamIds(ownerId);
                if (!steamIds.Any()) continue;
                Log.Trace("owner is player");

                var faction = MySession.Static.Factions.GetPlayerFaction(ownerId);
                var factionId = faction?.FactionId ?? 0;
                var factionName = faction?.Name;
                var position = grid.PositionComp.GetPosition();
                var gridInfo = new DefenderGridInfo(grid.EntityId, grid.DisplayName, factionId, factionName, position, steamIds);
                Log.Trace($"defender: {gridInfo}");

                yield return gridInfo;
            }
        }
    }
}