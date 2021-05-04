using System.Collections.Generic;
using System.Linq;
using NLog;
using Utils.General;

namespace TorchAlert.Core.Proximity
{
    public sealed class ProximityAlertBuffer
    {
        public interface IConfig
        {
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly Dictionary<(ulong, long), ProximityAlert> _buffer; // key is (defender player id, offender grid id)

        public ProximityAlertBuffer(IConfig config)
        {
            _config = config;
            _buffer = new Dictionary<(ulong, long), ProximityAlert>();
        }

        public IEnumerable<ProximityAlert> Buffer(IEnumerable<ProximityAlert> alerts)
        {
            foreach (var alert in alerts)
            {
                var pair = (alert.SteamId, alert.Offender.GridId);

                if (_buffer.ContainsKey(pair)) continue;
                Log.Trace($"first time alert: {alert}");

                _buffer[pair] = alert;
                yield return alert;
            }

            // forget about old matches
            var newAlerts = alerts.Select(a => (a.SteamId, a.Offender.GridId));
            _buffer.IntersectWith(newAlerts);
        }
    }
}