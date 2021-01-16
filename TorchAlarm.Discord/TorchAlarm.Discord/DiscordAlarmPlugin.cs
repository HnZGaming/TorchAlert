using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchAlarm.Core;
using Utils.General;
using Utils.Torch;

namespace TorchAlarm.Discord
{
    public sealed class DiscordAlarmPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<DiscordAlarmConfig> _config;
        UserControl _userControl;
        CancellationTokenSource _cancellationTokenSource;
        ProximityScanner _proximityScanner;
        DiscordBridge _discordBridge;
        ProximityReportMaker _reportMaker;

        public DiscordAlarmConfig Config => _config.Data;

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
            _config = Persistent<DiscordAlarmConfig>.Load(configPath);
        }

        public override void Dispose()
        {
            base.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        void OnGameLoaded()
        {
            _proximityScanner = new ProximityScanner(Config);
            _reportMaker = new ProximityReportMaker(
                MySession.Static.Factions,
                MySession.Static.Players);

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
                    // collect grids
                    var groups = MyCubeGridGroups.Static.Logical.Groups;
                    var grids = groups
                        .Select(g => g.GroupData.Root)
                        .Select(g => MakeGridInfo(g))
                        .ToArray();

                    // scan proximity
                    var scan = _proximityScanner.ScanProximity(grids);

                    // collect reports
                    _reportMaker.Clear();
                    _reportMaker.AddRange(scan);

                    // report
                    _discordBridge?.Dispose();
                    _discordBridge = new DiscordBridge(Config);
                    await _discordBridge.Send(_reportMaker.GetReports());
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

        GridInfo MakeGridInfo(MyCubeGrid grid)
        {
            return new GridInfo(
                grid.EntityId,
                grid.DisplayName,
                MySession.Static.Factions.GetOwnerFactionIdOrNull(grid),
                grid.PositionComp.GetPosition(),
                grid.IsStatic);
        }

        void OnGameUnloading()
        {
        }
    }
}