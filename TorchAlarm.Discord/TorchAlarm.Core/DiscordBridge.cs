using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using Utils.General;

namespace TorchAlarm.Core
{
    public sealed class DiscordBridge : IDisposable
    {
        public interface IConfig
        {
            string Token { get; }
        }

        readonly IConfig _config;
        readonly DiscordClient _client;

        public DiscordBridge(IConfig config)
        {
            _config = config;

            var discordConfig = new DiscordConfiguration
            {
                AutoReconnect = true,
                HttpTimeout = 10.Seconds(),
                LogLevel = LogLevel.Warning,
                ReconnectIndefinitely = true,
                Token = _config.Token,
                TokenType = TokenType.Bot,
            };

            _client = new DiscordClient(discordConfig);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task Send(IEnumerable<ProximityReport> reports)
        {
            await _client.ConnectAsync();
        }
    }
}