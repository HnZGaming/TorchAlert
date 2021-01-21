using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Discord.Torch
{
    public sealed class DiscordIdentityLinkDb
    {
        readonly string _filePath;
        readonly string _separator = "\t";
        readonly char[] _separators = {'\t'};
        readonly Dictionary<ulong, ulong> _steamToDiscord;
        readonly Dictionary<ulong, ulong> _discordToSteam;

        public DiscordIdentityLinkDb(string filePath)
        {
            _filePath = filePath;
            _steamToDiscord = new Dictionary<ulong, ulong>();
            _discordToSteam = new Dictionary<ulong, ulong>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Read()
        {
            _steamToDiscord.Clear();
            _discordToSteam.Clear();

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "");
                return;
            }

            foreach (var line in File.ReadAllLines(_filePath))
            {
                var ids = line.Split(_separators);

                if (ids.Length == 2 &&
                    ulong.TryParse(ids[0], out var steamId) &&
                    ulong.TryParse(ids[1], out var discordId))
                {
                    _steamToDiscord[steamId] = discordId;
                    _discordToSteam[discordId] = steamId;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Write()
        {
            var lines = new List<string>();
            foreach (var (steamId, discordId) in _steamToDiscord)
            {
                var line = $"{steamId}{_separator}{discordId}";
                lines.Add(line);
            }

            File.WriteAllLines(_filePath, lines);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Update(ulong steamId, ulong discordId)
        {
            _steamToDiscord[steamId] = discordId;
            _discordToSteam[discordId] = steamId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetDiscordId(ulong steamId, out ulong discordId)
        {
            return _steamToDiscord.TryGetValue(steamId, out discordId);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetSteamId(ulong discordId, out ulong steamId)
        {
            return _discordToSteam.TryGetValue(discordId, out steamId);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool HasDiscordLink(ulong steamId)
        {
            return _steamToDiscord.ContainsKey(steamId);
        }
    }
}