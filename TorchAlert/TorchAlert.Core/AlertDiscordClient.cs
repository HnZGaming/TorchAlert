using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Torch;
using NLog;
using TorchAlert.Damage;
using TorchAlert.Proximity;
using Utils.General;

namespace TorchAlert.Core
{
    public sealed class AlertDiscordClient
    {
        public interface IConfig
        {
            string ProximityAlertFormat { get; }
            string DamageAlertFormat { get; }
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

        public async Task SendDamageAlertAsync(IEnumerable<DamageAlert> allAlerts)
        {
            if (!allAlerts.Any()) return;

            // key: steam id; value: list of reports to that player
            var linkedAlerts = new Dictionary<ulong, List<DamageAlert>>();
            foreach (var alert in allAlerts)
            {
                linkedAlerts.Add(alert.SteamId, alert);
            }

            foreach (var (steamId, alerts) in linkedAlerts)
            {
                try
                {
                    var message = MakeDamageAlertMessage(alerts);
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
            var alertBuilder = new StringBuilder();
            foreach (var alert in alerts)
            {
                var msg = _config.ProximityAlertFormat
                    .Replace("{alert_name}", alert.GridName)
                    .Replace("{distance}", $"{alert.Distance:0}")
                    .Replace("{grid_name}", alert.Offender.GridName)
                    .Replace("{owner_name}", alert.Offender.OwnerName ?? "<none>")
                    .Replace("{faction_name}", alert.Offender.FactionName ?? "<none>")
                    .Replace("{faction_tag}", alert.Offender.FactionTag ?? "<none>");

                alertBuilder.AppendLine(msg);
            }

            return alertBuilder.ToString();
        }

        public async Task SendMockAlertAsync(ulong steamId)
        {
            await SendProximityAlertAsync(new[]
            {
                new ProximityAlert(steamId, 0, "My Grid", 1000, new OffenderGridInfo(0, "Enemy Ship", "Enemy", null, "ENM", "Enemy Faction")),
                new ProximityAlert(steamId, 0, "My Grid", 2000, new OffenderGridInfo(0, "Enemy Drone", "Enemy", null, "ENM", "Enemy Faction")),
            });
        }

        string MakeDamageAlertMessage(IEnumerable<DamageAlert> alerts)
        {
            var alertBuilder = new StringBuilder();
            foreach (var alert in alerts)
            {
                var msg = _config.DamageAlertFormat
                    .Replace("{alert_name}", alert.GridName)
                    .Replace("{owner_name}", alert.OffenderName ?? "<none>")
                    .Replace("{faction_name}", alert.OffenderFactionName ?? "<none>")
                    .Replace("{faction_tag}", alert.OffenderFactionTag ?? "<none>");

                alertBuilder.AppendLine(msg);
            }

            return alertBuilder.ToString();
        }
    }
}
