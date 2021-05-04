using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace TorchAlert.Core.Proximity
{
    // must be testable
    public sealed class OffenderProximityScanner
    {
        public interface IConfig
        {
            int MaxProximity { get; }
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
                var lookout = _config.MaxProximity * 10;
                var sphere = new BoundingSphereD(defender.Position, lookout);
                var bb = BoundingBoxD.CreateFromSphere(sphere);
                var obb = MyOrientedBoundingBoxD.CreateFromBoundingBox(bb);
                MyGamePruningStructure.GetAllEntitiesInOBB(ref obb, tmpNearEntities);

                foreach (var entity in tmpNearEntities)
                {
                    Log.Trace($"near entity: \"{entity.DisplayName}\"");

                    if (TryGetOffender(entity, defender, out var offender))
                    {
                        yield return offender;
                    }
                }

                tmpNearEntities.Clear();
            }
        }

        bool TryGetOffender(IMyEntity entity, DefenderGridInfo defender, out OffenderProximityInfo proximity)
        {
            proximity = default;

            if (!(entity is MyCubeGrid offenderGrid)) return false;
            if (offenderGrid.EntityId == defender.GridId) return false;
            if (!offenderGrid.IsTopMostParent()) return false;
            if (!IsEnemyGrid(offenderGrid, defender)) return false;

            var position = offenderGrid.PositionComp.GetPosition();
            var distance = Vector3D.Distance(defender.Position, position);
            if (distance > _config.MaxProximity) return false;

            var offenderInfo = MakeOffenderGridInfo(offenderGrid);
            proximity = new OffenderProximityInfo(defender, offenderInfo, distance);
            Log.Trace($"offender: {proximity}");

            return true;
        }

        static bool IsEnemyGrid(MyCubeGrid offenderGrid, DefenderGridInfo defender)
        {
            var offenderFactions =
                offenderGrid
                    .BigOwners.Concat(offenderGrid.SmallOwners)
                    .Select(i => MySession.Static.Factions.GetPlayerFaction(i))
                    .Where(f => f != null)
                    .ToSet();

            var defenderPlayerIds =
                defender
                    .SteamIds
                    .Select(s => MySession.Static.Players.TryGetIdentityId(s))
                    .ToSet();

            foreach (var offenderFaction in offenderFactions)
            foreach (var defenderPlayerId in defenderPlayerIds)
            {
                if (!offenderFaction.Members.ContainsKey(defenderPlayerId) &&
                    !offenderFaction.IsFriendly(defenderPlayerId))
                {
                    Log.Trace($"enemy: <{defenderPlayerId}> to [{offenderFaction.Tag}] \"{offenderFaction.Name}\"");
                    return true;
                }

                Log.Trace($"friendly: <{defenderPlayerId}> to [{offenderFaction.Tag}] \"{offenderFaction.Name}\"");
            }

            return false;
        }

        static OffenderGridInfo MakeOffenderGridInfo(MyCubeGrid grid)
        {
            if (!grid.BigOwners.TryGetFirst(out var playerId))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, null, 0, null);
            }

            MySession.Static.Players.TryGetPlayerById(playerId, out var player);
            var playerName = player?.DisplayName;

            if (!MySession.Static.Factions.TryGetFactionByPlayerId(playerId, out var faction))
            {
                return new OffenderGridInfo(grid.EntityId, grid.DisplayName, playerName, 0, null);
            }

            return new OffenderGridInfo(grid.EntityId, grid.DisplayName, playerName, faction.FactionId, faction.Tag);
        }
    }
}