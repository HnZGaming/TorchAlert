using System.Collections.Generic;
using System.Xml.Serialization;
using Discord.Torch;
using NLog;
using Torch;
using Torch.Views;
using TorchAlert.Core;
using TorchAlert.Proximity;
using Utils.General;

namespace TorchAlert
{
    public sealed class TorchAlertConfig :
        ViewModel,
        OffenderProximityScanner.IConfig,
        AlertDiscordClient.IConfig,
        AlertableSteamIdExtractor.IConfig,
        FileLoggingConfigurator.IConfig,
        ProximityAlertBuffer.IConfig,
        TorchDiscordClient.IConfig
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
        string _alertFormat = "${alert_name}: spotted enemy grid \"${grid_name}\" in ${distance} meters, owned by [${faction_tag}] ${owner_name}";
        string _damageAlertFormat = "${alert_name}: attacked by [${faction_tag}] ${owner_name}";
        string _logFilePath = "Logs/TorchAlert-${shortdate}.log";
        bool _suppressWpfOutput;
        bool _enableLoggingTrace;
        bool _enableLoggingDebug;

        [XmlElement("Enable")]
        [Display(Name = "Enable", GroupName = OpGroupName, Order = 0)]
        public bool Enable
        {
            get => _enable;
            set => SetValue(ref _enable, value);
        }

        [XmlElement("ScanInterval")]
        [Display(Name = "Scan interval", GroupName = OpGroupName, Order = 1)]
        public int ScanInterval
        {
            get => _scanInterval;
            set => SetValue(ref _scanInterval, value);
        }

        [XmlElement("ProximityThreshold")]
        [Display(Name = "Proximity threshold (meters)", GroupName = OpGroupName, Order = 2)]
        public int ProximityThreshold
        {
            get => _proximityThreshold;
            set => SetValue(ref _proximityThreshold, value);
        }

        [XmlElement("MutedSteamIds")]
        [Display(Name = "Muted Steam IDs", GroupName = OpGroupName, Order = 3)]
        public List<ulong> MutedSteamIds
        {
            get => _mutedSteamIds;
            set => SetValue(ref _mutedSteamIds, value);
        }

        [XmlElement("Token")]
        [Display(Name = "Discord bot token", GroupName = DiscordGroupName, Order = 1)]
        public string Token
        {
            get => _token;
            set => SetValue(ref _token, value);
        }

        [XmlElement("ProximityAlertFormat")]
        [Display(Name = "Proximity alert format", GroupName = DiscordGroupName, Order = 2)]
        public string ProximityAlertFormat
        {
            get => _alertFormat;
            set => SetValue(ref _alertFormat, value);
        }

        [XmlElement("DamageAlertFormat")]
        [Display(Name = "Damage alert format", GroupName = DiscordGroupName, Order = 3)]
        public string DamageAlertFormat
        {
            get => _damageAlertFormat;
            set => SetValue(ref _damageAlertFormat, value);
        }

        [XmlElement("LogFilePath")]
        [Display(Name = "Log file path", GroupName = LogGroupName, Order = 0)]
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetValue(ref _logFilePath, value);
        }

        [XmlElement("SuppressWpfOutput")]
        [Display(Name = "Suppress console output", GroupName = LogGroupName, Order = 1)]
        public bool SuppressWpfOutput
        {
            get => _suppressWpfOutput;
            set => SetValue(ref _suppressWpfOutput, value);
        }

        [XmlElement("EnableLoggingTrace")]
        [Display(Name = "Output trace logs", GroupName = LogGroupName, Order = 2)]
        public bool EnableLoggingTrace
        {
            get => _enableLoggingTrace;
            set => SetValue(ref _enableLoggingTrace, value);
        }

        [XmlElement("EnableLoggingDebug")]
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