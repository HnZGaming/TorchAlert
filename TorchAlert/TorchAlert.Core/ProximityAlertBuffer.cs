using System.Collections.Generic;
using NLog;

namespace TorchAlert.Core
{
    public sealed class ProximityAlertBuffer
    {
        public interface IConfig
        {
            double BufferDistance { get; }
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
                var key = (alert.GridId, alert.Offender.GridId);
                if (!_buffer.TryGetValue(key, out var lastAlert))
                {
                    Log.Trace($"first time alert: {alert}");
                    _buffer[key] = alert;
                    yield return alert;
                    continue;
                }

                if (GetBufferScope(alert) != GetBufferScope(lastAlert))
                {
                    Log.Trace($"updated alert: {alert}");
                    _buffer[key] = alert;
                    yield return alert;
                }

                Log.Trace($"skipped alert: {alert}");
            }
        }

        int GetBufferScope(ProximityAlert alert)
        {
            return (int) (alert.Distance / _config.BufferDistance);
        }
    }
}