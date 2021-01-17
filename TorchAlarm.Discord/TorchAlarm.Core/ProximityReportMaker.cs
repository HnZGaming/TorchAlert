using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.World;
using VRage.Game;

namespace TorchAlarm.Core
{
    public sealed class ProximityReportMaker
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<ProximityReport> GetReports(IEnumerable<Proximity> proximities)
        {
            // for each steam id, for each grid id, make a report
            var _reports = new Dictionary<ulong, Dictionary<long, ProximityReport>>();

            foreach (var proximity in proximities)
            foreach (var report in GetProximityReports(proximity))
            {
                if (!_reports.TryGetValue(report.SteamId, out var reports))
                {
                    reports = new Dictionary<long, ProximityReport>();
                    _reports[report.SteamId] = reports;
                }

                reports[proximity.Offender.GridId] = report;
            }

            foreach (var (_, reports) in _reports)
            foreach (var (_, report) in reports)
            {
                yield return report;
            }
        }

        IEnumerable<ProximityReport> GetProximityReports(Proximity proximity)
        {
            var (defender, offender, distance) = proximity;

            // skip friendly ships
            if (defender.FactionId is long defenderFactionId &&
                offender.FactionId is long offenderFactionId)
            {
                var (relation, _) = MySession.Static.Factions.GetRelationBetweenFactions(defenderFactionId, offenderFactionId);
                if (relation != MyRelationsBetweenFactions.Enemies) yield break;
            }

            foreach (var steamId in defender.SteamIds)
            {
                yield return new ProximityReport(steamId, defender.GridName, offender, distance);
            }
        }
    }
}