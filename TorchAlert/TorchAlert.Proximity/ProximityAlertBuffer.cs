using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NLog;
using Utils.General;

namespace TorchAlert.Proximity
{
    public sealed class ProximityAlertBuffer
    {
        public interface IConfig
        {
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly Dictionary<(long, long), ProximityAlert> _buffer;

        public ProximityAlertBuffer(IConfig config)
        {
            _config = config;
            _buffer = new Dictionary<(long, long), ProximityAlert>();
        }

        public IEnumerable<ProximityAlert> Buffer(IEnumerable<ProximityAlert> alerts)
        {
            foreach (var alert in alerts)
            {
                var pair = (alert.GridId, alert.Offender.GridId);
                if (_buffer.ContainsKey(pair))
                {
                    Log.Trace($"skipped alert: {alert}");
                    continue;
                }

                Log.Trace($"first time alert: {alert}");
                _buffer[pair] = alert;
                yield return alert;
            }

            var newAlerts = alerts.Select(a => (a.GridId, a.Offender.GridId));
            _buffer.IntersectWith(newAlerts);
        }
    }
}