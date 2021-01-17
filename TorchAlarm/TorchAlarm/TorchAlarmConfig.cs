using System.Collections.Generic;
using System.Xml.Serialization;
using NLog;
using Torch;
using Torch.Views;
using TorchAlarm.Core;
using TorchAlarm.Discord;
using Utils.General;

namespace TorchAlarm
{
    public sealed class TorchAlarmConfig :
        ViewModel,
        ProximityScanner.IConfig,
        DiscordAlarmClient.IConfig,
        GridInfoCollector.IConfig,
        FileLoggingConfigurator.IConfig,
        ProximityAlarmBuffer.IConfig
    {
        const string OpGroupName = "Operation";
        const string LogGroupName = "Logging";
        const string DiscordGroupName = "Discord";
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        int _scanInterval = 20;
        int _proximityThreshold = 10000;
        string _token = "empty";
        bool _enable = true;
        List<ulong> _mutedSteamIds = new List<ulong>();
        string _alarmFormat = "{alarm_name}: spotted \"{grid_name}\" in {distance} meters, owned by [{faction_tag}] {owner_name}";
        string _logFilePath = "Logs/TorchAlarm-${shortdate}.log";
        bool _suppressWpfOutput;
        bool _enableLoggingTrace;
        int _bufferCount = 3;

        [XmlElement("Enable")]
        [Display(Name = "Enable", GroupName = OpGroupName)]
        public bool Enable
        {
            get => _enable;
            set => SetValue(ref _enable, value);
        }

        [XmlElement("ScanInterval")]
        [Display(Name = "Scan interval", GroupName = OpGroupName)]
        public int ScanInterval
        {
            get => _scanInterval;
            set => SetValue(ref _scanInterval, value);
        }

        [XmlElement("ProximityThreshold")]
        [Display(Name = "Proximity threshold (meters)", GroupName = OpGroupName)]
        public int ProximityThreshold
        {
            get => _proximityThreshold;
            set => SetValue(ref _proximityThreshold, value);
        }

        [XmlElement("BufferCount")]
        [Display(Name = "Buffer count", GroupName = OpGroupName)]
        public int BufferCount
        {
            get => _bufferCount;
            set => SetValue(ref _bufferCount, value);
        }

        [XmlElement("Token")]
        [Display(Name = "Discord bot token", GroupName = DiscordGroupName)]
        public string Token
        {
            get => _token;
            set => SetValue(ref _token, value);
        }

        [XmlElement("AlarmFormat")]
        [Display(Name = "Alarm format", GroupName = DiscordGroupName)]
        public string AlarmFormat
        {
            get => _alarmFormat;
            set => SetValue(ref _alarmFormat, value);
        }

        [XmlElement("MutedSteamIds")]
        [Display(Name = "Muted Steam IDs", GroupName = OpGroupName)]
        public List<ulong> MutedSteamIds
        {
            get => _mutedSteamIds;
            set => SetValue(ref _mutedSteamIds, value);
        }

        [XmlElement("LogFilePath")]
        [Display(Name = "Log file path", GroupName = LogGroupName)]
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetValue(ref _logFilePath, value);
        }

        [XmlElement("SuppressWpfOutput")]
        [Display(Name = "Suppress console output", GroupName = LogGroupName)]
        public bool SuppressWpfOutput
        {
            get => _suppressWpfOutput;
            set => SetValue(ref _suppressWpfOutput, value);
        }

        [XmlElement("EnableLoggingTrace")]
        [Display(Name = "Output trace logs", GroupName = LogGroupName)]
        public bool EnableLoggingTrace
        {
            get => _enableLoggingTrace;
            set => SetValue(ref _enableLoggingTrace, value);
        }

        public bool IsMuted(ulong steamId)
        {
            return _mutedSteamIds.Contains(steamId);
        }

        public void Mute(ulong steamId)
        {
            if (!_mutedSteamIds.Contains(steamId))
            {
                Log.Info($"muted: {steamId}");
                _mutedSteamIds.Add(steamId);
                OnPropertyChanged(nameof(MutedSteamIds));
            }
        }

        public void Unmute(ulong steamId)
        {
            if (_mutedSteamIds.Remove(steamId))
            {
                Log.Info($"unmuted: {steamId}");
                OnPropertyChanged(nameof(MutedSteamIds));
            }
        }

        double ProximityAlarmBuffer.IConfig.BufferDistance => (double) _proximityThreshold / _bufferCount;
    }
}