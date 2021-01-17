using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Sandbox.Game.World;
using VRage.Game;

namespace TorchAlarm.Core
{
    public sealed class ProximityAlarmCreator
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<ProximityAlarm> CreateAlarms(IEnumerable<Proximity> proximities)
        {
            // for each steam id, for each grid id, make a report
            var allAlarms = new Dictionary<ulong, Dictionary<long, ProximityAlarm>>();

            foreach (var proximity in proximities)
            foreach (var alarm in GetProximityAlarms(proximity))
            {
                if (!allAlarms.TryGetValue(alarm.SteamId, out var alarms))
                {
                    alarms = new Dictionary<long, ProximityAlarm>();
                    allAlarms[alarm.SteamId] = alarms;
                }

                alarms[proximity.Offender.GridId] = alarm;
            }

            foreach (var (_, alarms) in allAlarms)
            foreach (var (_, alarm) in alarms)
            {
                Log.Trace($"created alarm: {alarm}");
                yield return alarm;
            }
        }

        IEnumerable<ProximityAlarm> GetProximityAlarms(Proximity proximity)
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
                yield return new ProximityAlarm(steamId, defender.GridName, distance, offender);
            }
        }
    }
}