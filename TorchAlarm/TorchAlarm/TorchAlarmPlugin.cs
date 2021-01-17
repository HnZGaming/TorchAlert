using System;
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
        ProximityAlarmMaker _alarmMaker;
        DiscordAlarmClient _discordClient;
        DiscordIdentityLinker _idendityLinker;

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

            _fileLoggingConfigurator = new FileLoggingConfigurator(nameof(TorchAlarm), "TorchAlarm.*", Config.LogFilePath);
            _fileLoggingConfigurator.Initialize();
            _fileLoggingConfigurator.Reconfigure(Config);

            var linkerFilePath = this.MakeFilePath($"{nameof(DiscordIdentityLinker)}.sqlite");
            _idendityLinker = new DiscordIdentityLinker(linkerFilePath);

            _defenderGridCollector = new GridInfoCollector(Config);
            _proximityScanner = new ProximityScanner(Config);
            _alarmMaker = new ProximityAlarmMaker();
            _discordClient = new DiscordAlarmClient(Config, _idendityLinker);
        }

        void OnGameLoaded()
        {
            TaskUtils.RunUntilCancelledAsync(MainLoop, _cancellationTokenSource.Token);
        }

        async Task MainLoop(CancellationToken cancellationToken)
        {
            Log.Debug("Start main loop");

            _idendityLinker.Initialize();
            await _discordClient.ConnectAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Config.Enable)
                {
                    await Task.Delay(10.Seconds(), cancellationToken);
                    continue;
                }

                try
                {
                    Log.Debug("Start main loop interval");

                    var grids = _defenderGridCollector.CollectDefenderGrids();
                    var scan = _proximityScanner.ScanProximity(grids);
                    var alarms = _alarmMaker.MakeAlarms(scan);

                    foreach (var alarm in alarms)
                    {
                        Log.Trace($"alarm: {alarm}");
                    }

                    await _discordClient.SendAlarmsAsync(alarms);

                    Log.Debug("Finished main loop interval");
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

        void OnGameUnloading()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            _discordClient?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        public int GenerateLinkId(ulong steamId)
        {
            return _idendityLinker.GenerateLinkId(steamId);
        }
    }
}