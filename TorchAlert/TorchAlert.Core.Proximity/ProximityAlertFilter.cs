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
        readonly Dictionary<ulong, HashSet<long>> _lastAlertGridIds;

        public ProximityAlertFilter(IConfig config, IParentsLookupTree<long> splitLookup)
        {
            _config = config;
            _splitLookup = splitLookup;
            _lastAlertGridIds = new Dictionary<ulong, HashSet<long>>();
        }

        public IEnumerable<ProximityAlert> Filter(IEnumerable<ProximityAlert> alerts)
        {
            var alertsPerPlayer = new Dictionary<ulong, HashSet<ProximityAlert>>();
            var offenderGridIdsPerPlayer = new Dictionary<ulong, HashSet<long>>();
            foreach (var alert in alerts)
            {
                alertsPerPlayer.Add(alert.SteamId, alert);
                offenderGridIdsPerPlayer.Add(alert.SteamId, alert.Offender.GridId);
            }

            // forget about offender grids that didn't show up once
            _lastAlertGridIds.IntersectWith(offenderGridIdsPerPlayer);

            foreach (var (steamId, playerAlerts) in alertsPerPlayer)
            {
                var lastAlertGridIds = _lastAlertGridIds.GetOrAdd<ulong, long, HashSet<long>>(steamId);
                foreach (var playerAlert in playerAlerts)
                {
                    var alertGridId = playerAlert.Offender.GridId;
                    if (lastAlertGridIds.Contains(alertGridId))
                    {
                        Log.Trace($"filtered alert: already alerted: {playerAlert}");
                        continue;
                    }

                    var parentGridIds = _splitLookup.GetParentsOf(alertGridId);
                    if (lastAlertGridIds.ContainsAny(parentGridIds))
                    {
                        Log.Trace($"filtered alert: parent(s) already alerted: {playerAlert}");
                        continue;
                    }

                    _lastAlertGridIds.Add(steamId, alertGridId);
                    yield return playerAlert;
                }
            }

            foreach (var (steamId, lastAlertGridIds) in _lastAlertGridIds)
            {
                Log.Debug($"last alerts to {steamId}: {lastAlertGridIds.ToStringSeq()}");
            }
        }
    }
}