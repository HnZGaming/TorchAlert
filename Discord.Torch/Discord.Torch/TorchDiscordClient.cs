using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLog;
using Sandbox.Game.World;
using Torch.API.Managers;
using Utils.General;
using Utils.Torch;

namespace Discord.Torch
{
    public sealed class TorchDiscordClient
    {
        public interface IConfig
        {
            string Token { get; }
            bool EnableGameText { get; }
            string GameText { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DiscordSocketClient _client;
        readonly DiscordIdentityLinker _identityLinker;
        readonly List<ITorchDiscordMessageListener> _messageListeners;
        IChatManagerServer _chatManager;

        public TorchDiscordClient(IConfig config, DiscordIdentityLinker identityLinker)
        {
            _config = config;
            _identityLinker = identityLinker;
            _messageListeners = new List<ITorchDiscordMessageListener>();

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

        public void AddMessageListener(ITorchDiscordMessageListener messageListener)
        {
            _messageListeners.Add(messageListener);
        }

        public async Task ConnectAsync()
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
            await UpdateGameTextAsync();

            IsReady = true;
            Log.Info("connected");
        }

        public async Task UpdateGameTextAsync()
        {
            if (_config.EnableGameText)
            {
                await _client.SetGameAsync(_config.GameText);
            }
        }

        public void Close()
        {
            _client.MessageReceived -= OnMessageReceivedAsync;
            _client.Dispose();
            _messageListeners.Clear();
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
            var receivedMessage = content.ToLower();
            if (receivedMessage.Contains("check"))
            {
                if (_identityLinker.TryGetSteamId(user.Id, out var linkedSteamId))
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

            if (int.TryParse(receivedMessage, out var linkId))
            {
                Log.Info($"link id: {linkId}");

                if (_identityLinker.TryMakeLink(linkId, user.Id, out var linkedSteamId))
                {
                    Log.Info($"linked steam ID: {linkedSteamId}");

                    var linkedPlayerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(linkedSteamId);
                    await channel.MentionAsync(user.Id, $"Alert linked to \"{linkedPlayerName}\".");
                    _chatManager.SendMessage("Alert", linkedSteamId, $"Alert linked to \"{user.Username}\".");
                    return;
                }

                await channel.MentionAsync(user.Id, $"Invalid input; not mapped: {linkId}");
                return;
            }

            // no linked steam id found
            if (!_identityLinker.TryGetSteamId(user.Id, out var steamId))
            {
                await channel.MentionAsync(user.Id, "Steam ID not linked");
                return;
            }

            // delegate to listeners
            foreach (var messageListener in _messageListeners)
            {
                if (messageListener.TryRespond(steamId, receivedMessage, out var response))
                {
                    await channel.MentionAsync(user.Id, response);
                    return;
                }
            }

            await channel.MentionAsync(user.Id, $"Unknown message: \"{receivedMessage}\"");
        }

        public async Task SendDirectMessageAsync(ulong steamId, string message)
        {
            if (_identityLinker.TryGetDiscordId(steamId, out var discordId))
            {
                var discordUser = await _client.Rest.GetUserAsync(discordId);
                await discordUser.SendMessageAsync(message);
            }
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