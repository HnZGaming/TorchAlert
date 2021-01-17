using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NLog;
using Sandbox.Game.World;
using TorchAlarm.Core;
using Utils.General;
using Utils.Torch;

namespace TorchAlarm.Discord
{
    public sealed class DiscordAlarmClient : IDisposable
    {
        public interface IConfig
        {
            string Token { get; }
            string AlarmFormat { get; }

            void Mute(ulong steamId);
            void Unmute(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DiscordSocketClient _client;
        readonly DiscordIdentityLinker _identityLinker;

        public DiscordAlarmClient(IConfig config, DiscordIdentityLinker identityLinker)
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

        public async Task InitializeAsync()
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
            if (int.TryParse(msg, out var linkId))
            {
                Log.Info($"link id: {linkId}");

                if (_identityLinker.TryMakeLink(linkId, user.Id, out var linkedSteamId))
                {
                    Log.Info($"linked steam ID: {linkedSteamId}");

                    var linkedPlayerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(linkedSteamId);
                    await channel.MentionAsync(user.Id, $"Alarm linked to \"{linkedPlayerName}\". Say \"mute\" and \"unmute\" to turn on/off alarms.");
                    return;
                }

                await channel.MentionAsync(user.Id, $"Invalid input; not mapped: {linkId}");
            }

            if (!_identityLinker.TryGetLinkedSteamUser(user.Id, out var steamId))
            {
                await channel.MentionAsync(user.Id, "Alarm not linked to you; type `!alarm link` in game to get started");
                return;
            }

            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);

            if (msg.Contains("start") || msg.Contains("unmute"))
            {
                _config.Unmute(steamId);
                await channel.MentionAsync(user.Id, $"Alarm started by \"{playerName}\"");
                return;
            }

            if (msg.Contains("stop") || msg.Contains("mute"))
            {
                _config.Mute(steamId);
                await channel.MentionAsync(user.Id, $"Alarm stopped by \"{playerName}\"");
                return;
            }

            await channel.MentionAsync(user.Id, "wot?");
        }

        public async Task SendAlarmsAsync(IEnumerable<ProximityAlarm> allAlarms)
        {
            if (!allAlarms.Any()) return;

            // key: discord id; value: list of reports to that discord user
            var linkedAlarms = new Dictionary<ulong, List<ProximityAlarm>>();
            foreach (var alarm in allAlarms)
            {
                if (_identityLinker.TryGetLinkedDiscordId(alarm.SteamId, out var discordId))
                {
                    linkedAlarms.Add(discordId, alarm);
                    Log.Trace($"linked: {alarm}");
                }
                else
                {
                    Log.Trace($"not linked: {alarm}");
                }
            }

            foreach (var (discordId, alarms) in linkedAlarms)
            {
                try
                {
                    var discordUser = await _client.Rest.GetUserAsync(discordId);
                    var message = MakeAlarmMessage(alarms);
                    await discordUser.SendMessageAsync(message);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        string MakeAlarmMessage(IEnumerable<ProximityAlarm> alarms)
        {
            var alarmBuilder = new StringBuilder();
            foreach (var alarm in alarms)
            {
                var msg = _config.AlarmFormat
                    .Replace("{alarm_name}", alarm.GridName)
                    .Replace("{distance}", $"{alarm.Distance:0}")
                    .Replace("{grid_name}", alarm.Offender.GridName)
                    .Replace("{owner_name}", alarm.Offender.OwnerName ?? "<none>")
                    .Replace("{faction_name}", alarm.Offender.FactionName ?? "<none>")
                    .Replace("{faction_tag}", alarm.Offender.FactionTag ?? "<none>");

                alarmBuilder.AppendLine(msg);
            }

            return alarmBuilder.ToString();
        }
    }
}