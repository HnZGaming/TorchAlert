using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.Torch;
using VRage.Game.Entity;
using VRageMath;

namespace TorchAlarm.Core
{
    // must be testable
    public sealed class ProximityScanner
    {
        public interface IConfig
        {
            int ProximityThreshold { get; }
        }

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

                    var factionId = MySession.Static.Factions.GetOwnerFactionIdOrNull(nearGrid);
                    var gridInfo = new OffenderGridInfo(nearGrid.EntityId, factionId);
                    var proximity = new Proximity(defender, gridInfo, distance);
                    yield return proximity;
                }

                tmpNearEntities.Clear();
            }
        }
    }
}