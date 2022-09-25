using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Torch;
using NLog;
using TorchAlert.Core.Proximity;
using Utils.General;

namespace TorchAlert.Core
{
    public sealed class AlertDiscordClient
    {
        public interface IConfig
        {
            string ProximityAlertFormat { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly TorchDiscordClient _client;

        public AlertDiscordClient(IConfig config, TorchDiscordClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task SendProximityAlertAsync(IEnumerable<ProximityAlert> allAlerts)
        {
            if (!allAlerts.Any()) return;

            // key: steam id; value: list of reports to that player
            var linkedAlerts = new Dictionary<ulong, List<ProximityAlert>>();
            foreach (var alert in allAlerts)
            {
                linkedAlerts.Add(alert.SteamId, alert);
                Log.Debug($"Sending alert: {{{alert}}}");
            }

            foreach (var (steamId, alerts) in linkedAlerts)
            {
                try
                {
                    var message = MakeProximityAlertMessage(alerts);
                    await _client.SendDirectMessageAsync(steamId, message);
                }
                catch (Exception e) // can happen if discord user isn't found etc
                {
                    Log.Error(e);
                }
            }
        }

        string MakeProximityAlertMessage(IEnumerable<ProximityAlert> alerts)
        {
            var messages = new HashSet<string>();
            foreach (var alert in alerts)
            {
                var message = _config.ProximityAlertFormat
                    .Replace("${distance}", $"{alert.Distance:0}")
                    .Replace("${grid_name_ours}", alert.DefenderGridName)
                    .Replace("${grid_name_theirs}", alert.Offender.GridName)
                    .Replace("${owner_name}", alert.Offender.OwnerName ?? "<none>")
                    .Replace("${faction_tag}", alert.Offender.FactionTag ?? "<none>");

                messages.Add(message);
            }

            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine(message);
            }

            return sb.ToString();
        }

        public async Task SendMockAlertAsync(ulong steamId)
        {
            await SendProximityAlertAsync(new[]
            {
                new ProximityAlert(steamId, 0, "My Grid", new OffenderGridInfo(0, "Enemy Ship", "Enemy", 0, "ENM"), 2000),
                new ProximityAlert(steamId, 0, "My Grid", new OffenderGridInfo(0, "Enemy Drone", "Enemy", 0, "ENM"), 2000),
            });
        }
    }
}