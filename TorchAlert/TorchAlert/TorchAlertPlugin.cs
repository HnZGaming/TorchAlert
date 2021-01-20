using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Discord.Torch;
using NLog;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using TorchAlert.Core;
using TorchAlert.Damage;
using TorchAlert.Proximity;
using Utils.General;
using Utils.Torch;

namespace TorchAlert
{
    public sealed class TorchAlertPlugin : TorchPluginBase, IWpfPlugin, ITorchDiscordMessageListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchAlertConfig> _config;
        UserControl _userControl;
        FileLoggingConfigurator _fileLoggingConfigurator;
        CancellationTokenSource _cancellationTokenSource;
        AlertableSteamIdExtractor _steamIdExtractor;
        GridInfoCollector _defenderGridCollector;
        ProximityScanner _proximityScanner;
        ProximityAlertCreator _alertCreator;
        ProximityAlertBuffer _alertBuffer;
        TorchDiscordClient _torchDiscordClient;
        AlertDiscordClient _alertDiscordClient;
        DiscordIdentityLinker _identityLinker;
        DiscordIdentityLinkDb _linkDb;
        DamageInfoQueue _damageInfoQueue;

        public TorchAlertConfig Config => _config.Data;
        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _cancellationTokenSource = new CancellationTokenSource();

            var configPath = this.MakeConfigFilePath();
            _config = Persistent<TorchAlertConfig>.Load(configPath);
            Config.PropertyChanged += OnConfigPropertyChanged;

            _fileLoggingConfigurator = new FileLoggingConfigurator(nameof(TorchAlert), new[] {"TorchAlert.*", "Discord.Net.*"}, Config.LogFilePath);
            _fileLoggingConfigurator.Initialize();
            _fileLoggingConfigurator.Configure(Config);

            var linkDbPath = this.MakeFilePath($"{nameof(DiscordIdentityLinker)}.csv");
            _linkDb = new DiscordIdentityLinkDb(linkDbPath);
            _steamIdExtractor = new AlertableSteamIdExtractor(Config, _linkDb);
            _defenderGridCollector = new GridInfoCollector(_steamIdExtractor);
            _proximityScanner = new ProximityScanner(Config);
            _alertCreator = new ProximityAlertCreator();
            _alertBuffer = new ProximityAlertBuffer(Config);
            _identityLinker = new DiscordIdentityLinker(_linkDb);
            _torchDiscordClient = new TorchDiscordClient(Config, _identityLinker);
            _alertDiscordClient = new AlertDiscordClient(Config, _torchDiscordClient);
            _damageInfoQueue = new DamageInfoQueue();

            Log.Info("initialized");
        }

        async void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                if (Config.Enable)
                {
                    if (args.PropertyName == nameof(Config.Enable) ||
                        args.PropertyName == nameof(Config.Token))
                    {
                        await InitializeDiscordAsync();
                    }
                }

                if (args.PropertyName == nameof(Config.LogFilePath) ||
                    args.PropertyName == nameof(Config.EnableLoggingTrace))
                {
                    _fileLoggingConfigurator.Configure(Config);
                }
            }
            catch (Exception e)
            {
                Log.Warn(e, "failed applying config changes");
            }
        }

        void OnGameLoaded()
        {
            var chatManager = Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
            _torchDiscordClient.Initialize(chatManager);
            _torchDiscordClient.AddMessageListener(this);

            _linkDb.Read();
            _damageInfoQueue.Initialize();

            TaskUtils.RunUntilCancelledAsync(MainLoop, _cancellationTokenSource.Token).Forget(Log);
        }

        async Task MainLoop(CancellationToken cancellationToken)
        {
            Log.Info("start main loop");

            if (Config.Enable)
            {
                await InitializeDiscordAsync();
            }

            _damageInfoQueue.Clear();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Config.Enable || !_torchDiscordClient.IsReady)
                {
                    await Task.Delay(10.Seconds(), cancellationToken);
                    _damageInfoQueue.Clear();
                    continue;
                }

                Log.Debug("start main loop interval");

                try
                {
                    var grids = _defenderGridCollector.CollectDefenderGrids().ToArray();
                    var scan = _proximityScanner.ScanProximity(grids).ToArray();
                    var alerts = _alertCreator.CreateAlerts(scan).ToArray();
                    alerts = _alertBuffer.Buffer(alerts).ToArray();

                    foreach (var alert in alerts)
                    {
                        Log.Trace($"alert: {alert}");
                    }

                    await _alertDiscordClient.SendProximityAlertAsync(alerts);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                using (_damageInfoQueue.DequeueDamageInfos(out var damageInfos))
                {
                    var alerts = new Dictionary<ulong, List<DamageAlert>>();
                    foreach (var damageInfo in damageInfos)
                    {
                        var steamIds = _steamIdExtractor.GetAlertableSteamIds(damageInfo.DefenderId);
                        foreach (var steamId in steamIds)
                        {
                            var gridName = damageInfo.DefenderGridName;
                            var isPlayer = MySession.Static.Players.TryGetPlayerById(damageInfo.OffenderId, out var offender);
                            var offenderName = isPlayer ? offender.DisplayName : null;
                            var hasFaction = MySession.Static.Factions.TryGetPlayerFaction(damageInfo.OffenderId, out var faction);
                            var factionName = hasFaction ? faction.Name : null;
                            var factionTag = hasFaction ? faction.Tag : null;
                            var alert = new DamageAlert(steamId, gridName, offenderName, factionName, factionTag);

                            alerts.Add(steamId, alert);
                        }
                    }

                    var a = alerts.Values.SelectMany(v => v);
                    await _alertDiscordClient.SendDamageAlertAsync(a);
                }

                Log.Debug("finished main loop interval");
                await Task.Delay(Config.ScanInterval.Seconds(), cancellationToken);
            }
        }

        async Task InitializeDiscordAsync()
        {
            try
            {
                await _torchDiscordClient.ConnectAsync();
                Log.Info("discord connected");
            }
            catch (Exception e)
            {
                Log.Warn(e, "failed connecting discord; dry-run until config is changed");
            }
        }

        void OnGameUnloading()
        {
            _torchDiscordClient?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Config.PropertyChanged -= OnConfigPropertyChanged;
            _config.Dispose();
        }

        public int GenerateLinkId(ulong steamId)
        {
            return _identityLinker.GenerateLinkId(steamId);
        }

        public async Task<(bool, string)> TryGetLinkedDiscordUserName(ulong steamId)
        {
            if (!_identityLinker.TryGetDiscordId(steamId, out var discordId))
            {
                return (false, default);
            }

            return await _torchDiscordClient.TryGetDiscordUserName(discordId);
        }

        public Task SendMockAlert(ulong steamId)
        {
            return _alertDiscordClient.SendMockAlertAsync(steamId);
        }

        bool ITorchDiscordMessageListener.TryRespond(ulong steamId, string message, out string response)
        {
            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);

            if (message.Contains("start") || message.Contains("unmute"))
            {
                Config.Unmute(steamId);
                response = $"Alerts started by \"{playerName}\"";
                return true;
            }

            if (message.Contains("stop") || message.Contains("mute"))
            {
                Config.Mute(steamId);
                response = $"Alerts stopped by \"{playerName}\"";
                return true;
            }

            response = null;
            return false;
        }

        public async Task SendDiscordMessageAsync(ulong steamId, string message)
        {
            await _torchDiscordClient.SendMessageAsync(steamId, message);
        }
    }
}