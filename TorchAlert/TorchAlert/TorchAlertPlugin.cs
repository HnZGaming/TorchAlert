using System;
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
        DefenderGridCollector _defenderGridCollector;
        OffenderProximityScanner _offenderProximityScanner;
        ProximityAlertCreator _alertCreator;
        ProximityAlertBuffer _alertBuffer;
        TorchDiscordClient _torchDiscordClient;
        AlertDiscordClient _alertDiscordClient;
        DiscordIdentityLinker _identityLinker;
        DiscordIdentityLinkDb _linkDb;
        DamageInfoQueue _damageInfoQueue;
        DamageAlertCreator _damageAlertCreator;

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
            _defenderGridCollector = new DefenderGridCollector(_steamIdExtractor);
            _offenderProximityScanner = new OffenderProximityScanner(Config);
            _alertCreator = new ProximityAlertCreator();
            _alertBuffer = new ProximityAlertBuffer(Config);
            _identityLinker = new DiscordIdentityLinker(_linkDb);
            _torchDiscordClient = new TorchDiscordClient(Config, _identityLinker);
            _alertDiscordClient = new AlertDiscordClient(Config, _torchDiscordClient);
            _damageInfoQueue = new DamageInfoQueue();
            _damageAlertCreator = new DamageAlertCreator(_steamIdExtractor);

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
                    args.PropertyName == nameof(Config.SuppressWpfOutput) ||
                    args.PropertyName == nameof(Config.EnableLoggingTrace) ||
                    args.PropertyName == nameof(Config.EnableLoggingDebug))
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
                    Log.Debug($"defenders: {grids.ToStringSeq()}");

                    var scan = _offenderProximityScanner.ScanProximity(grids).ToArray();
                    Log.Debug($"proximities: {scan.ToStringSeq()}");

                    var alerts = _alertCreator.CreateAlerts(scan).ToArray();
                    alerts = _alertBuffer.Buffer(alerts).ToArray();
                    Log.Debug($"alerts: {alerts.ToStringSeq()}");

                    await _alertDiscordClient.SendProximityAlertAsync(alerts);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                using (_damageInfoQueue.DequeueDamageInfos(out var damageInfos))
                {
                    Log.Debug($"damaged grids: {damageInfos.ToStringSeq()}");
                    var damageAlerts = _damageAlertCreator.CreateDamageAlerts(damageInfos);
                    Log.Debug($"damage alerts: {damageAlerts.ToStringSeq()}");
                    await _alertDiscordClient.SendDamageAlertAsync(damageAlerts);
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
            if (!_linkDb.TryGetDiscordId(steamId, out var discordId))
            {
                var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                throw new Exception($"Discord not linked to steam user: \"{playerName}\" ({steamId})");
            }

            await _torchDiscordClient.SendMessageAsync(discordId, message);
        }
    }
}