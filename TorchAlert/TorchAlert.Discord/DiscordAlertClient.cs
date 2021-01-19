using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NLog;
using Sandbox.Game.World;
using Torch.API.Managers;
using TorchAlert.Core;
using Utils.General;
using Utils.Torch;

namespace TorchAlert.Discord
{
    public sealed class DiscordAlertClient : IDisposable
    {
        public interface IConfig
        {
            string Token { get; }
            string AlertFormat { get; }

            void Mute(ulong steamId);
            void Unmute(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DiscordSocketClient _client;
        readonly DiscordIdentityLinker _identityLinker;
        IChatManagerServer _chatManager;

        public DiscordAlertClient(IConfig config, DiscordIdentityLinker identityLinker)
        {
            _config = config;
            _identityLinker = identityLinker;

            var discordConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                ConnectionTimeout = (int) 10.Seconds().TotalMilliseconds,
                HandlerTimeout = (int) 10.Seconds().TotalMilliseconds,
            };

            _client = new DiscordSocketClient(discordConfig);
            _client.MessageReceived += OnMessageReceivedAsync;
        }

        public bool IsReady { get; private set; }

        public void Initialize(IChatManagerServer chatManager)
        {
            _chatManager = chatManager;
        }

        public async Task ConnectAsnc()
        {
            Log.Info("connecting...");
            IsReady = false;

            try
            {
                await _client.StopAsync();
            }
            catch (Exception e)
            {
                Log.Trace(e, "ignoring");
            }

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync("Watching...");

            IsReady = true;
            Log.Info("connected");
        }

        public void Dispose()
        {
            _client.MessageReceived -= OnMessageReceivedAsync;
            _client.Dispose();
        }

        async Task OnMessageReceivedAsync(SocketMessage er)
        {
            if (er.Author.IsBot) return;

            var e = er as SocketUserMessage ?? throw new Exception("invalid message type");
            Log.Debug($"bot message received: {e.Channel.Name} \"{e.Content}\"");

            try
            {
                if (e.Channel is SocketDMChannel) // direct message
                {
                    Log.Info($"bot direct message received: \"{e.Content}\"");
                    await OnMessageReceivedAsync(e.Channel, e.Author, e.Content);
                }
                else if (e.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
                {
                    Log.Info($"bot mentioned message received: \"{e.Content}\"");
                    var content = DiscordUtils.RemoveMentionPrefix(e.Content);
                    await OnMessageReceivedAsync(e.Channel, e.Author, content);
                }
            }
            catch (Exception ex)
            {
                CommandErrorResponseGenerator.LogAndRespond(this, ex, msg =>
                {
                    e.Channel.SendMessageAsync(msg).Wait();
                });
            }
        }

        async Task OnMessageReceivedAsync(ISocketMessageChannel channel, SocketUser user, string content)
        {
            Log.Info($"input: \"{content}\" by user: {user.Username} in channel: {channel.Name}");

            var msg = content.ToLower();
            if (msg.Contains("check"))
            {
                if (_identityLinker.TryGetLinkedSteamUser(user.Id, out var linkedSteamId))
                {
                    var linkedPlayerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(linkedSteamId);
                    await channel.MentionAsync(user.Id, $"Your Discord user is linked to \"{linkedPlayerName}\".");
                    _chatManager.SendMessage("Alert", linkedSteamId, $"You're linked to \"{user.Username}\".");
                }
                else
                {
                    await channel.MentionAsync(user.Id, "Your Discord user is not linked to any in-game players. Try `!alert link` in game.");
                }

                return;
            }

            if (int.TryParse(msg, out var linkId))
            {
                Log.Info($"link id: {linkId}");

                if (_identityLinker.TryMakeLink(linkId, user.Id, out var linkedSteamId))
                {
                    Log.Info($"linked steam ID: {linkedSteamId}");

                    var linkedPlayerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(linkedSteamId);
                    await channel.MentionAsync(user.Id, $"Alert linked to \"{linkedPlayerName}\". Say \"mute\" and \"unmute\" to turn on/off alerts.");
                    _chatManager.SendMessage("Alert", linkedSteamId, $"Alert linked to \"{user.Username}\". Type \"!mute\" and \"!unmute\" to turn on/off alerts.");
                    return;
                }

                await channel.MentionAsync(user.Id, $"Invalid input; not mapped: {linkId}");
                return;
            }

            if (!_identityLinker.TryGetLinkedSteamUser(user.Id, out var steamId))
            {
                await channel.MentionAsync(user.Id, "Alerts not linked to you; type `!alert link` in game to get started");
                return;
            }

            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);

            if (msg.Contains("start") || msg.Contains("unmute"))
            {
                _config.Unmute(steamId);
                await channel.MentionAsync(user.Id, $"Alerts started by \"{playerName}\"");
                return;
            }

            if (msg.Contains("stop") || msg.Contains("mute"))
            {
                _config.Mute(steamId);
                await channel.MentionAsync(user.Id, $"Alerts stopped by \"{playerName}\"");
                return;
            }

            await channel.MentionAsync(user.Id, "wot?");
        }

        public async Task SendAlertAsync(IEnumerable<ProximityAlert> allAlerts)
        {
            if (!allAlerts.Any()) return;

            // key: discord id; value: list of reports to that discord user
            var linkedAlerts = new Dictionary<ulong, List<ProximityAlert>>();
            foreach (var alert in allAlerts)
            {
                if (_identityLinker.TryGetLinkedDiscordId(alert.SteamId, out var discordId))
                {
                    linkedAlerts.Add(discordId, alert);
                    Log.Trace($"linked: {alert}");
                }
                else
                {
                    Log.Trace($"not linked: {alert}");
                }
            }

            foreach (var (discordId, alerts) in linkedAlerts)
            {
                try
                {
                    var discordUser = await _client.Rest.GetUserAsync(discordId);
                    var message = MakeAlertMessage(alerts);
                    await discordUser.SendMessageAsync(message);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        string MakeAlertMessage(IEnumerable<ProximityAlert> alerts)
        {
            var alertBuilder = new StringBuilder();
            foreach (var alert in alerts)
            {
                var msg = _config.AlertFormat
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

        public async Task<(bool, string)> TryGetDiscordUserName(ulong discordUserId)
        {
            var user = await _client.Rest.GetUserAsync(discordUserId);
            return (user != null, user?.Username);
        }

        public async Task SendMessageAsync(ulong discordUserId, string message)
        {
            var user = await _client.Rest.GetUserAsync(discordUserId);
            if (user == null)
            {
                throw new Exception($"Discord user not found by Discord user ID: {discordUserId}");
            }

            await user.SendMessageAsync(message);
        }
    }
}