using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchAlert.Core;
using TorchAlert.Discord;
using Utils.General;
using Utils.Torch;

namespace TorchAlert
{
    public sealed class TorchAlertPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchAlertConfig> _config;
        UserControl _userControl;
        FileLoggingConfigurator _fileLoggingConfigurator;
        CancellationTokenSource _cancellationTokenSource;
        GridInfoCollector _defenderGridCollector;
        ProximityScanner _proximityScanner;
        ProximityAlertCreator _alertCreator;
        ProximityAlertBuffer _alertBuffer;
        DiscordAlertClient _discordClient;
        DiscordIdentityLinker _identityLinker;
        DiscordIdentityLinkDb _linkDb;

        public TorchAlertConfig Config => _config.Data;

        public UserControl GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

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

            _defenderGridCollector = new GridInfoCollector(Config);
            _proximityScanner = new ProximityScanner(Config);
            _alertCreator = new ProximityAlertCreator();
            _alertBuffer = new ProximityAlertBuffer(Config);
            _identityLinker = new DiscordIdentityLinker(_linkDb);
            _discordClient = new DiscordAlertClient(Config, _identityLinker);

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
            TaskUtils.RunUntilCancelledAsync(MainLoop, _cancellationTokenSource.Token).Forget(Log);
        }

        async Task MainLoop(CancellationToken cancellationToken)
        {
            Log.Info("start main loop");

            _linkDb.Initialize();

            if (Config.Enable)
            {
                await InitializeDiscordAsync();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Config.Enable || !_discordClient.IsReady)
                {
                    await Task.Delay(10.Seconds(), cancellationToken);
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

                    await _discordClient.SendAlertAsync(alerts);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                Log.Debug("finished main loop interval");
                await Task.Delay(Config.ScanInterval.Seconds(), cancellationToken);
            }
        }

        async Task InitializeDiscordAsync()
        {
            try
            {
                await _discordClient.InitializeAsync();
                Log.Info("discord connected");
            }
            catch (Exception e)
            {
                Log.Warn(e, "failed connecting discord; dry-run until config is changed");
            }
        }

        void OnGameUnloading()
        {
            _discordClient?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Config.PropertyChanged -= OnConfigPropertyChanged;
            _config.Dispose();
        }

        public int GenerateLinkId(ulong steamId)
        {
            return _identityLinker.GenerateLinkId(steamId);
        }
    }
}