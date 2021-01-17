using System.Collections.Generic;

namespace TorchAlarm.Core
{
    public sealed class ProximityAlarmBuffer
    {
        public interface IConfig
        {
            double BufferDistance { get; }
        }

        readonly IConfig _config;
        readonly Dictionary<(long, long), ProximityAlarm> _buffer;

        public ProximityAlarmBuffer(IConfig config)
        {
            _config = config;
            _buffer = new Dictionary<(long, long), ProximityAlarm>();
        }

        public IEnumerable<ProximityAlarm> Buffer(IEnumerable<ProximityAlarm> alarms)
        {
            foreach (var alarm in alarms)
            {
                var key = (alarm.GridId, alarm.Offender.GridId);
                if (!_buffer.TryGetValue(key, out var lastAlarm))
                {
                    _buffer[key] = alarm;
                    yield return alarm;
                    continue;
                }

                if (GetBufferScope(alarm) != GetBufferScope(lastAlarm))
                {
                    _buffer[key] = alarm;
                    yield return alarm;
                }
            }
        }

        int GetBufferScope(ProximityAlarm alarm)
        {
            return (int) (alarm.Distance / _config.BufferDistance);
        }
    }
}