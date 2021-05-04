using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Torch;
using NLog;
using Sandbox.Game.World;
using Torch.API.Managers;
using TorchAlert.Core.Proximity;
using Utils.General;

namespace TorchAlert.Core
{
    public sealed class TorchAlert : ITorchDiscordMessageListener
    {
        public interface IConfig :
            OffenderProximityScanner.IConfig,
            AlertDiscordClient.IConfig,
            AlertableSteamIdExtractor.IConfig,
            ProximityAlertBuffer.IConfig,
            TorchDiscordClient.IConfig
        {
            bool Enable { get; }
            int ScanInterval { get; }

            void Mute(ulong steamId);
            void Unmute(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DefenderGridCollector _defenderGridCollector;
        readonly OffenderProximityScanner _offenderProximityScanner;
        readonly ProximityAlertCreator _alertCreator;
        readonly ProximityAlertBuffer _alertBuffer;
        readonly TorchDiscordClient _torchDiscordClient;
        readonly AlertDiscordClient _alertDiscordClient;
        readonly DiscordIdentityLinker _identityLinker;
        readonly DiscordIdentityLinkDb _linkDb;

        public TorchAlert(IConfig config, string linkDbPath)
        {
            _config = config;
            _linkDb = new DiscordIdentityLinkDb(linkDbPath);
            var steamIdExtractor = new AlertableSteamIdExtractor(_config, _linkDb);
            _defenderGridCollector = new DefenderGridCollector(steamIdExtractor);
            _offenderProximityScanner = new OffenderProximityScanner(_config);
            _alertCreator = new ProximityAlertCreator();
            _alertBuffer = new ProximityAlertBuffer(_config);
            _identityLinker = new DiscordIdentityLinker(_linkDb);
            _torchDiscordClient = new TorchDiscordClient(_config, _identityLinker);
            _alertDiscordClient = new AlertDiscordClient(_config, _torchDiscordClient);
        }

        public void Initialize(IChatManagerServer chatManager)
        {
            _torchDiscordClient.Initialize(chatManager);
            _torchDiscordClient.AddMessageListener(this);

            _linkDb.Read();
        }

        public void Close()
        {
            _torchDiscordClient?.Close();
        }

        public async Task Main(CancellationToken cancellationToken)
        {
            Log.Info("start main loop");

            while (!_config.Enable)
            {
                await Task.Delay(1.Seconds(), cancellationToken);
            }

            await InitializeDiscordAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_config.Enable || !_torchDiscordClient.IsReady)
                {
                    await Task.Delay(1.Seconds(), cancellationToken);
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

                Log.Debug("finished main loop interval");
                await Task.Delay(_config.ScanInterval.Seconds(), cancellationToken);
            }
        }

        public async Task InitializeDiscordAsync()
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
                _config.Unmute(steamId);
                response = $"Alerts started by \"{playerName}\"";
                return true;
            }

            if (message.Contains("stop") || message.Contains("mute"))
            {
                _config.Mute(steamId);
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