using System.Collections.Generic;
using System.Xml.Serialization;
using NLog;
using Torch;
using Torch.Views;
using Utils.Torch;

namespace TorchAlert
{
    public sealed class TorchAlertConfig :
        ViewModel,
        FileLoggingConfigurator.IConfig,
        Core.TorchAlert.IConfig
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
        string _alertFormat = "Enemy grid spotted: [${faction_tag}] \"${grid_name}\"";
        string _logFilePath = "Logs/TorchAlert-${shortdate}.log";
        bool _suppressWpfOutput;
        bool _enableLoggingTrace;
        bool _enableLoggingDebug;
        bool _enableGameText = true;
        string _gameText = "Watching";

        [XmlElement]
        [Display(Name = "Enable", GroupName = OpGroupName, Order = 0)]
        public bool Enable
        {
            get => _enable;
            set => SetValue(ref _enable, value);
        }

        [XmlElement]
        [Display(Name = "Scan interval", GroupName = OpGroupName, Order = 1)]
        public int ScanInterval
        {
            get => _scanInterval;
            set => SetValue(ref _scanInterval, value);
        }

        [XmlElement]
        [Display(Name = "Proximity threshold (meters)", GroupName = OpGroupName, Order = 2)]
        public int MaxProximity
        {
            get => _proximityThreshold;
            set => SetValue(ref _proximityThreshold, value);
        }

        [XmlElement]
        [Display(Name = "Muted Steam IDs", GroupName = OpGroupName, Order = 3)]
        public List<ulong> MutedSteamIds
        {
            get => _mutedSteamIds;
            set => SetValue(ref _mutedSteamIds, value);
        }

        [XmlElement]
        [Display(Name = "Discord bot token", GroupName = DiscordGroupName, Order = 1)]
        public string Token
        {
            get => _token;
            set => SetValue(ref _token, value);
        }

        [XmlElement]
        [Display(Name = "Proximity alert format", GroupName = DiscordGroupName, Order = 2)]
        public string ProximityAlertFormat
        {
            get => _alertFormat;
            set => SetValue(ref _alertFormat, value);
        }

        [XmlElement]
        [Display(Name = "Enable game text", GroupName = DiscordGroupName, Order = 3)]
        public bool EnableGameText
        {
            get => _enableGameText;
            set => SetValue(ref _enableGameText, value);
        }

        [XmlElement]
        [Display(Name = "Game text", GroupName = DiscordGroupName, Order = 4)]
        public string GameText
        {
            get => _gameText;
            set => SetValue(ref _gameText, value);
        }

        [XmlElement]
        [Display(Name = "Log file path", GroupName = LogGroupName, Order = 0)]
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetValue(ref _logFilePath, value);
        }

        [XmlElement]
        [Display(Name = "Suppress console output", GroupName = LogGroupName, Order = 1)]
        public bool SuppressWpfOutput
        {
            get => _suppressWpfOutput;
            set => SetValue(ref _suppressWpfOutput, value);
        }

        [XmlElement]
        [Display(Name = "Output trace logs", GroupName = LogGroupName, Order = 2)]
        public bool EnableLoggingTrace
        {
            get => _enableLoggingTrace;
            set => SetValue(ref _enableLoggingTrace, value);
        }

        [XmlElement]
        [Display(Name = "Output debug logs", GroupName = LogGroupName, Order = 3)]
        public bool EnableLoggingDebug
        {
            get => _enableLoggingDebug;
            set => SetValue(ref _enableLoggingDebug, value);
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
    }
}