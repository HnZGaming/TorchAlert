using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
            string AlarmFormat { get; }

            void Mute(ulong steamId);
            void Unmute(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DiscordClient _client;
        readonly DiscordIdentityLinker _identityLinker;
        readonly Dictionary<ulong, DiscordMember> _discordMembers;
        DiscordGuild _guild;

        public DiscordAlarmClient(string token, IConfig config, DiscordIdentityLinker identityLinker)
        {
            _config = config;
            _identityLinker = identityLinker;
            _discordMembers = new Dictionary<ulong, DiscordMember>();

            var discordConfig = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                ReconnectIndefinitely = false,
                HttpTimeout = 5.Seconds(),
            };

            _client = new DiscordClient(discordConfig);
            _client.MessageCreated += OnMessageCreatedAsync;
        }

        public bool IsReady { get; private set; }

        public async Task ConnectAsync()
        {
            Log.Info("connecting...");

            var activity = new DiscordActivity("Watching...", ActivityType.Playing);

            await _client
                .ConnectAsync(activity, UserStatus.Online) // throws if already connected
                .Timeout(5.Seconds()); // because the shit code won't give up

            Log.Info("connected");
        }

        public async Task LoadGuildAsync(ulong guildId)
        {
            Log.Info("loading guild...");
            IsReady = false;

            _guild = await _client.GetGuildAsync(guildId);
            if (_guild == null)
            {
                throw new Exception("guild not found");
            }

            IsReady = true;
            Log.Info("loaded guild");
        }

        public void Dispose()
        {
            _client.MessageCreated -= OnMessageCreatedAsync;
            //_client.Dispose(); // commented out because the shit code calls Dispose on GC
            _discordMembers.Clear();
        }

        async Task OnMessageCreatedAsync(MessageCreateEventArgs e)
        {
            Log.Debug($"bot message received: {e.Channel.Type} \"{e.Message.Content}\"");

            try
            {
                if (e.Channel.Type == ChannelType.Private) // direct message
                {
                    await OnPrivateMessageCreatedAsync(e);
                }
            }
            catch (Exception ex)
            {
                CommandErrorResponseGenerator.LogAndRespond(this, ex, msg =>
                {
                    e.Message.RespondAsync(msg).Wait();
                });
            }
        }

        async Task OnPrivateMessageCreatedAsync(MessageCreateEventArgs e)
        {
            Log.Info($"bot private message received: \"{e.Message.Content}\"");

            var msg = e.Message.Content.ToLower();
            if (int.TryParse(msg, out var linkId))
            {
                Log.Info($"link id: {linkId}");

                if (_identityLinker.TryMakeLink(linkId, e.Author.Id, out var linkedSteamId))
                {
                    Log.Info($"linked steam ID: {linkedSteamId}");

                    var linkedPlayerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(linkedSteamId);
                    await e.Message.RespondAsync($"Alarm linked to \"{linkedPlayerName}\" ({linkedSteamId})");
                    return;
                }

                await e.Message.RespondAsync($"Invalid input; not mapped: {linkId}");
            }

            if (!_identityLinker.TryGetLinkedSteamUser(e.Author.Id, out var steamId))
            {
                await e.Message.RespondAsync("Alarm not linked to you; type `!ta link` in game to get started");
                return;
            }

            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);

            if (msg.Contains("stop") || msg.Contains("mute"))
            {
                _config.Mute(steamId);
                await e.Message.RespondAsync($"Alarm muted to \"{playerName}\" ({steamId})");
                return;
            }

            if (msg.Contains("start") || msg.Contains("unmute"))
            {
                _config.Unmute(steamId);
                await e.Message.RespondAsync($"Alarm started for \"{playerName}\" ({steamId})");
                return;
            }

            await e.Message.RespondAsync("wot?");
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
                    Log.Trace($"linked alarm: {alarm}");
                }
                else
                {
                    Log.Trace($"not linked alarm: {alarm}");
                }
            }

            foreach (var (discordId, alarms) in linkedAlarms)
            {
                var discordMember = await GetDiscordMemberAsync(discordId);
                var message = MakeAlarmMessage(alarms);
                await discordMember.SendMessageAsync(message);
            }
        }

        async Task<DiscordMember> GetDiscordMemberAsync(ulong discordId)
        {
            if (!_discordMembers.TryGetValue(discordId, out var discordMember))
            {
                discordMember = await _guild.GetMemberAsync(discordId);
                _discordMembers[discordId] = discordMember;
            }

            return discordMember;
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