using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace TorchAlarm.Core
{
    public sealed class GridInfoCollector
    {
        public interface IConfig
        {
            bool IsMuted(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;

        public GridInfoCollector(IConfig config)
        {
            _config = config;
        }

        public IEnumerable<DefenderGridInfo> CollectDefenderGrids()
        {
            var groups = MyCubeGridGroups.Static.Logical.Groups;
            foreach (var group in groups)
            {
                var grid = group.GroupData.Root;
                if (TryMakeDefenderGridInfo(grid, out var gridInfo))
                {
                    yield return gridInfo;
                }
            }
        }

        bool TryMakeDefenderGridInfo(IMyCubeGrid grid, out DefenderGridInfo gridInfo)
        {
            gridInfo = null;

            Log.Trace($"\"{grid.DisplayName}\" static: {grid.IsStatic}");

            if (!grid.IsStatic) return false;

            var steamIds = new HashSet<ulong>();
            GetSteamIdsFromGrid(grid, steamIds);

            Log.Trace($"alarm receiver steam IDs: {steamIds.ToStringSeq()}");

            steamIds.RemoveWhere(id => _config.IsMuted(id));

            Log.Trace($"after removing muted players: {steamIds.ToStringSeq()}");

            if (!steamIds.Any()) return false;

            var factionId = MySession.Static.Factions.GetOwnerFactionOrNull(grid)?.FactionId;
            gridInfo = new DefenderGridInfo(grid.EntityId, grid.DisplayName, factionId, grid.GetPosition(), steamIds);

            Log.Trace($"grid info: {gridInfo}");

            return true;
        }

        static void GetSteamIdsFromGrid(IMyCubeGrid grid, ICollection<ulong> steamIds)
        {
            if (grid.BigOwners.TryGetFirst(out var ownerId))
            {
                var faction = MySession.Static.Factions.TryGetPlayerFaction(ownerId);
                if (faction != null)
                {
                    var factionId = faction.FactionId;
                    foreach (var memberSteamId in GetSteamIdsFromFactionId(factionId))
                    {
                        steamIds.Add(memberSteamId);
                    }

                    return;
                }

                if (TryGetSteamIdFromPlayerId(ownerId, out var steamId))
                {
                    steamIds.Add(steamId);
                }
            }
        }

        static bool TryGetSteamIdFromPlayerId(long playerId, out ulong steamId)
        {
            steamId = MySession.Static.Players.TryGetSteamId(playerId);
            return steamId != 0;
        }

        static IEnumerable<ulong> GetSteamIdsFromFactionId(long factionId)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(factionId);
            if (faction == null) yield break;

            foreach (var (_, member) in faction.Members)
            {
                var playerId = member.PlayerId;
                if (TryGetSteamIdFromPlayerId(playerId, out var steamId))
                {
                    yield return steamId;
                }
            }
        }
    }
}