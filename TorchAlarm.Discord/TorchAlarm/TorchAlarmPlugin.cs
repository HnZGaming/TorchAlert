using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchAlarm.Core;
using Utils.General;
using Utils.Torch;

namespace TorchAlarm
{
    public sealed class TorchAlarmPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchAlarmConfig> _config;
        UserControl _userControl;
        CancellationTokenSource _cancellationTokenSource;
        GridInfoCollector _defenderGridCollector;
        ProximityScanner _proximityScanner;
        ProximityReportMaker _reportMaker;
        DiscordBridge _discordBridge;

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

            _defenderGridCollector = new GridInfoCollector(Config);
            _proximityScanner = new ProximityScanner(Config);
            _reportMaker = new ProximityReportMaker();
        }

        void OnGameLoaded()
        {
            TaskUtils.RunUntilCancelledAsync(MainLoop, _cancellationTokenSource.Token);
        }

        async Task MainLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Config.Enable)
                {
                    await Task.Delay(10.Seconds(), cancellationToken);
                    continue;
                }

                try
                {
                    var grids = _defenderGridCollector.CollectDefenderGrids();
                    var scan = _proximityScanner.ScanProximity(grids);
                    var reports = _reportMaker.GetReports(scan);

                    _discordBridge?.Dispose();
                    _discordBridge = new DiscordBridge(Config);
                    await _discordBridge.Send(reports);
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}