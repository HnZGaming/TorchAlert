using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.Multiplayer;
using VRage.Game;

namespace TorchAlarm.Core
{
    public sealed class ProximityReportMaker
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly Dictionary<ulong, Dictionary<long, ProximityReport>> _reports;
        readonly MyFactionCollection _factions;
        readonly MyPlayerCollection _players;

        public ProximityReportMaker(MyFactionCollection factions, MyPlayerCollection players)
        {
            _factions = factions;
            _players = players;
            _reports = new Dictionary<ulong, Dictionary<long, ProximityReport>>();
        }

        public IEnumerable<ProximityReport> GetReports()
        {
            foreach (var (_, reports) in _reports)
            foreach (var (_, report) in reports)
            {
                yield return report;
            }
        }

        public void Clear()
        {
            _reports.Clear();
        }

        public void AddRange(IEnumerable<Proximity> proximities)
        {
            foreach (var proximity in proximities)
            {
                Add(proximity);
            }
        }

        public void Add(Proximity proximity)
        {
            foreach (var report in GetProximityReports(proximity))
            {
                if (!_reports.TryGetValue(report.SteamId, out var reports))
                {
                    reports = new Dictionary<long, ProximityReport>();
                    _reports[report.SteamId] = reports;
                }

                reports[report.GridId] = report;
            }
        }

        IEnumerable<ProximityReport> GetProximityReports(Proximity proximity)
        {
            var empty = Enumerable.Empty<ProximityReport>();
            var (g0, g1, distance) = proximity;
            if (!g0.IsStatic && !g1.IsStatic) return empty;

            if (g0.FactionId is long g0FactionId &&
                g1.FactionId is long g1FactionId)
            {
                var (relation, _) = _factions.GetRelationBetweenFactions(g0FactionId, g1FactionId);
                if (relation != MyRelationsBetweenFactions.Enemies) return empty;
            }

            if (g0.FactionId.HasValue && g0.IsStatic)
            {
                return GetProximityReports(g0, g1, distance);
            }

            if (g1.FactionId.HasValue && g1.IsStatic)
            {
                return GetProximityReports(g1, g0, distance);
            }

            return empty;
        }

        IEnumerable<ProximityReport> GetProximityReports(GridInfo defenderGrid, GridInfo offenderGrid, double distance)
        {
            var steamIds = GetSteamIdsFromFaction(defenderGrid.FactionId.Value);
            foreach (var steamId in steamIds)
            {
                yield return new ProximityReport(
                    steamId,
                    defenderGrid.GridId,
                    defenderGrid.GridName,
                    offenderGrid.FactionId,
                    distance);
            }
        }

        IEnumerable<ulong> GetSteamIdsFromFaction(long factionId)
        {
            var faction = _factions.TryGetFactionById(factionId);
            if (faction == null) yield break;

            foreach (var (_, member) in faction.Members)
            {
                var playerId = member.PlayerId;
                var steamId = _players.TryGetSteamId(playerId);
                Log.Info($"{playerId} -> {steamId}");
                if (steamId > 0)
                {
                    yield return steamId;
                }
            }
        }
    }
}