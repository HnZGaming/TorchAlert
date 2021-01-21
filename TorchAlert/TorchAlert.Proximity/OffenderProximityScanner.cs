using System.Collections.Generic;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;
using VRage.Game.Entity;
using VRageMath;

namespace TorchAlert.Proximity
{
    // must be testable
    public sealed class OffenderProximityScanner
    {
        public interface IConfig
        {
            int ProximityThreshold { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;

        public OffenderProximityScanner(IConfig config)
        {
            _config = config;
        }

        public IEnumerable<OffenderProximityInfo> ScanProximity(IEnumerable<DefenderGridInfo> defenders)
        {
            var tmpNearEntities = new List<MyEntity>();

            foreach (var defender in defenders)
            {
                var lookout = _config.ProximityThreshold * 10;
                var sphere = new BoundingSphereD(defender.Position, lookout);
                var bb = BoundingBoxD.CreateFromSphere(sphere);
                var obb = MyOrientedBoundingBoxD.CreateFromBoundingBox(bb);
                MyGamePruningStructure.GetAllEntitiesInOBB(ref obb, tmpNearEntities);

                foreach (var entity in tmpNearEntities)
                {
                    if (!(entity is MyCubeGrid grid)) continue;
                    if (grid.EntityId == defender.GridId) continue;
                    if (!grid.IsTopMostParent<MyCubeGrid>()) continue;

                    var position = grid.PositionComp.GetPosition();
                    var distance = Vector3D.Distance(defender.Position, position);
                    if (distance > _config.ProximityThreshold) continue;

                    var offenderInfo = MakeOffenderGridInfo(grid);
                    var proximity = new OffenderProximityInfo(defender, offenderInfo, distance);

                    Log.Trace($"proximity: {proximity}");
                    yield return proximity;
                }

                tmpNearEntities.Clear();
            }
        }

        static OffenderGridInfo MakeOffenderGridInfo(MyCubeGrid grid)
        {
            if (!grid.BigOwners.TryGetFirst(out var playerId))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, null, null, null);
            }

            var playerName = MySession.Static.Players.TryGetPlayerById(playerId, out var player)
                ? player.DisplayName
                : null; // maybe NPC

            if (!MySession.Static.Factions.TryGetPlayerFaction(playerId, out var faction))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, playerName, null, null);
            }

            return new OffenderGridInfo(grid.EntityId, grid.DisplayName, playerName, faction.FactionId, faction.Tag);
        }
    }
}