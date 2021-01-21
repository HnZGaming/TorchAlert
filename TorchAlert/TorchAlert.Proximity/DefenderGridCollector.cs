using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using TorchAlert.Core;
using Utils.General;

namespace TorchAlert.Proximity
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
            var groups = MyCubeGridGroups.Static.Logical.Groups;
            foreach (var group in groups)
            {
                var grid = group.GroupData.Root;
                Log.Trace($"\"{grid.DisplayName}\" static: {grid.IsStatic}");
                if (!grid.IsStatic) continue;

                if (grid.BigOwners.TryGetFirst(out var ownerId))
                {
                    var steamIds = _steamIdExtractor.GetAlertableSteamIds(ownerId);
                    if (!steamIds.Any()) continue;

                    var factionId = MySession.Static.Factions.GetPlayerFaction(ownerId)?.FactionId;
                    var position = grid.PositionComp.GetPosition();
                    var gridInfo = new DefenderGridInfo(grid.EntityId, grid.DisplayName, factionId, position, steamIds);

                    Log.Trace($"defender: {gridInfo}");
                    yield return gridInfo;
                }
            }
        }
    }
}