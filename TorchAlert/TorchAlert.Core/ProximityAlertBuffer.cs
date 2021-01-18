using System.Collections.Generic;

namespace TorchAlert.Core
{
    public sealed class ProximityAlertBuffer
    {
        public interface IConfig
        {
            double BufferDistance { get; }
        }

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
                    _buffer[key] = alert;
                    yield return alert;
                    continue;
                }

                if (GetBufferScope(alert) != GetBufferScope(lastAlert))
                {
                    _buffer[key] = alert;
                    yield return alert;
                }
            }
        }

        int GetBufferScope(ProximityAlert alert)
        {
            return (int) (alert.Distance / _config.BufferDistance);
        }
    }
}