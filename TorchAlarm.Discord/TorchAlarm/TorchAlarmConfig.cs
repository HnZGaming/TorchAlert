using System.Collections.Generic;
using System.Xml.Serialization;
using Torch;
using Torch.Views;
using TorchAlarm.Core;

namespace TorchAlarm
{
    public sealed class TorchAlarmConfig :
        ViewModel,
        ProximityScanner.IConfig,
        DiscordBridge.IConfig,
        GridInfoCollector.IConfig
    {
        int _scanInterval = 20;
        int _proximityThreshold = 5000;
        string _token;
        bool _enable = true;
        List<ulong> _mutedSteamIds;

        [XmlElement("Enable")]
        [Display(Name = "Enable")]
        public bool Enable
        {
            get => _enable;
            set => _enable = value;
        }

        [XmlElement("ScanInterval")]
        [Display(Name = "Scan interval")]
        public int ScanInterval
        {
            get => _scanInterval;
            set => SetValue(ref _scanInterval, value);
        }

        [XmlElement("ProximityThreshold")]
        [Display(Name = "Proximity threshold (meters)")]
        public int ProximityThreshold
        {
            get => _proximityThreshold;
            set => SetValue(ref _proximityThreshold, value);
        }

        [XmlElement("Token")]
        [Display(Name = "Discord bot token")]
        public string Token
        {
            get => _token;
            set => SetValue(ref _token, value);
        }

        [XmlElement("MutedSteamIds")]
        [Display(Name = "Muted Steam IDs")]
        public List<ulong> MutedSteamIds
        {
            get => _mutedSteamIds;
            set => SetValue(ref _mutedSteamIds, value);
        }

        public bool IsMuted(ulong steamId)
        {
            return _mutedSteamIds.Contains(steamId);
        }

        public void Mute(ulong steamId)
        {
            if (!_mutedSteamIds.Contains(steamId))
            {
                _mutedSteamIds.Add(steamId);
                OnPropertyChanged(nameof(MutedSteamIds));
            }
        }

        public void Unmute(ulong steamId)
        {
            if (_mutedSteamIds.Remove(steamId))
            {
                OnPropertyChanged(nameof(MutedSteamIds));
            }
        }
    }
}