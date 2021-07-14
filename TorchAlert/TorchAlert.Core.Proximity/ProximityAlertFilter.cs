using System.Collections.Generic;
using System.Linq;
using NLog;
using Utils.General;

namespace TorchAlert.Core.Proximity
{
    // try not to spam alerts (judging from the past)
    public sealed class ProximityAlertFilter
    {
        public interface IConfig
        {
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly IParentsLookupTree<long> _splitLookup;
        readonly Dictionary<ulong, HashSet<long>> _playersToLastOffenderIds;

        public ProximityAlertFilter(IConfig config, IParentsLookupTree<long> splitLookup)
        {
            _config = config;
            _splitLookup = splitLookup;
            _playersToLastOffenderIds = new Dictionary<ulong, HashSet<long>>();
        }

        public IEnumerable<ProximityAlert> Filter(IEnumerable<ProximityAlert> nextAlerts)
        {
            var playersToAlerts = new Dictionary<ulong, HashSet<ProximityAlert>>();
            var playersToOffenderIds = new Dictionary<ulong, HashSet<long>>();
            foreach (var nextAlert in nextAlerts)
            {
                playersToAlerts.Add(nextAlert.SteamId, nextAlert);
                playersToOffenderIds.Add(nextAlert.SteamId, nextAlert.Offender.GridId);
            }

            // forget about offender grids that didn't show up once
            // so that we won't hold onto distant/deleted grids forever
            // also we need to detect "re-entry"
            _playersToLastOffenderIds.IntersectWith(playersToOffenderIds);

            foreach (var (steamId, alerts) in playersToAlerts)
            {
                var lastOffenderIds = _playersToLastOffenderIds.GetOrAdd<ulong, long, HashSet<long>>(steamId);
                foreach (var alert in alerts)
                {
                    var offenderId = alert.Offender.GridId;
                    if (lastOffenderIds.Contains(offenderId))
                    {
                        Log.Debug($"filtered alert: already alerted: {alert}");
                        continue;
                    }

                    var parentGridIds = _splitLookup.GetParentsOf(offenderId);
                    if (lastOffenderIds.ContainsAny(parentGridIds))
                    {
                        Log.Debug($"filtered alert: parent(s) already alerted: {alert}");
                        continue;
                    }

                    _playersToLastOffenderIds.Add(steamId, offenderId);
                    yield return alert;
                }
            }

            foreach (var (steamId, offenderIds) in _playersToLastOffenderIds)
            {
                Log.Debug($"last alerts to {steamId}: {offenderIds.ToStringSeq()}");
            }
        }
    }
}