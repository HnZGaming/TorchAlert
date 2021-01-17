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
using TorchAlarm.Core;
using TorchAlarm.Discord;
using Utils.General;
using Utils.Torch;

namespace TorchAlarm
{
    public sealed class TorchAlarmPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchAlarmConfig> _config;
        UserControl _userControl;
        FileLoggingConfigurator _fileLoggingConfigurator;
        CancellationTokenSource _cancellationTokenSource;
        GridInfoCollector _defenderGridCollector;
        ProximityScanner _proximityScanner;
        ProximityAlarmCreator _alarmCreator;
        DiscordAlarmClient _discordClient;
        DiscordIdentityLinker _identityLinker;
        DiscordIdentityLinkDb _linkDb;

        public TorchAlarmConfig Config => _config.Data;

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
            _config = Persistent<TorchAlarmConfig>.Load(configPath);
            Config.PropertyChanged += OnConfigPropertyChanged;

            _fileLoggingConfigurator = new FileLoggingConfigurator(nameof(TorchAlarm), new[] {"TorchAlarm.*", "Discord.Net.*"}, Config.LogFilePath);
            _fileLoggingConfigurator.Initialize();
            _fileLoggingConfigurator.Reconfigure(Config);

            var linkDbPath = this.MakeFilePath($"{nameof(DiscordIdentityLinker)}.csv");
            _linkDb = new DiscordIdentityLinkDb(linkDbPath);

            _defenderGridCollector = new GridInfoCollector(Config);
            _proximityScanner = new ProximityScanner(Config);
            _alarmCreator = new ProximityAlarmCreator();
            _identityLinker = new DiscordIdentityLinker(_linkDb);
            _discordClient = new DiscordAlarmClient(Config, _identityLinker);

            Log.Info("initialized");
        }

        async void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                if (args.PropertyName == nameof(Config.Enable))
                {
                    if (Config.Enable)
                    {
                        await InitializeDiscordAsync();
                    }
                }

                if (args.PropertyName == nameof(Config.Token) && Config.Enable)
                {
                    InitializeDiscordAsync().Wait();
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

                try
                {
                    Log.Debug("start main loop interval");

                    var grids = _defenderGridCollector.CollectDefenderGrids().ToArray();
                    var scan = _proximityScanner.ScanProximity(grids).ToArray();
                    var alarms = _alarmCreator.CreateAlarms(scan).ToArray();

                    foreach (var alarm in alarms)
                    {
                        Log.Trace($"alarm: {alarm}");
                    }

                    await _discordClient.SendAlarmsAsync(alarms);

                    Log.Debug("finished main loop interval");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

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