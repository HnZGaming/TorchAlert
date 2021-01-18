using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.World;
using VRage.Game;

namespace TorchAlert.Core
{
    public sealed class ProximityAlertCreator
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<ProximityAlert> CreateAlerts(IEnumerable<Proximity> proximities)
        {
            // for each steam id, for each grid id, make a report
            var allAlerts = new Dictionary<ulong, Dictionary<long, ProximityAlert>>();

            foreach (var proximity in proximities)
            foreach (var alert in GetProximityAlerts(proximity))
            {
                if (!allAlerts.TryGetValue(alert.SteamId, out var alerts))
                {
                    alerts = new Dictionary<long, ProximityAlert>();
                    allAlerts[alert.SteamId] = alerts;
                }

                alerts[proximity.Offender.GridId] = alert;
            }

            foreach (var (_, alerts) in allAlerts)
            foreach (var (_, alert) in alerts)
            {
                Log.Trace($"created alert: {alert}");
                yield return alert;
            }
        }

        IEnumerable<ProximityAlert> GetProximityAlerts(Proximity proximity)
        {
            var (defender, offender, distance) = proximity;

            // skip friendly ships
            if (defender.FactionId is long defenderFactionId &&
                offender.FactionId is long offenderFactionId)
            {
                var (relation, _) = MySession.Static.Factions.GetRelationBetweenFactions(defenderFactionId, offenderFactionId);
                Log.Trace($"checking relationship: {relation}, {proximity}");
                if (relation != MyRelationsBetweenFactions.Enemies) yield break;
            }

            foreach (var steamId in defender.SteamIds)
            {
                yield return new ProximityAlert(steamId, defender.GridId, defender.GridName, distance, offender);
            }
        }
    }
}