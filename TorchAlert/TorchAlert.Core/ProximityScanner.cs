using System.Collections.Generic;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.Torch;
using VRage.Game.Entity;
using VRageMath;

namespace TorchAlert.Core
{
    // must be testable
    public sealed class ProximityScanner
    {
        public interface IConfig
        {
            int ProximityThreshold { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;

        public ProximityScanner(IConfig config)
        {
            _config = config;
        }

        public IEnumerable<Proximity> ScanProximity(IEnumerable<DefenderGridInfo> defenders)
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
                    if (!(entity is MyCubeGrid nearGrid)) continue;
                    if (nearGrid.EntityId == defender.GridId) continue;

                    var position = nearGrid.PositionComp.GetPosition();
                    var distance = Vector3D.Distance(defender.Position, position);
                    if (distance > _config.ProximityThreshold) continue;

                    var gridInfo = MakeOffenderGridInfo(nearGrid);
                    var proximity = new Proximity(defender, gridInfo, distance);

                    Log.Trace($"proximity: {proximity}");
                    yield return proximity;
                }

                tmpNearEntities.Clear();
            }
        }

        static OffenderGridInfo MakeOffenderGridInfo(MyCubeGrid grid)
        {
            if (!MySession.Static.Players.TryGetPlayerByGrid(grid, out var player))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, null, null, null, null);
            }

            if (!MySession.Static.Factions.TryGetFactionByPlayerId(player.PlayerId(), out var faction))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, player.DisplayName, null, null, null);
            }

            return new OffenderGridInfo(grid.EntityId, grid.DisplayName, player.DisplayName, faction.FactionId, faction.Tag, faction.Name);
        }
    }
}