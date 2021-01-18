using System.Collections.Generic;
using NLog;
using Utils.General;

namespace TorchAlert.Discord
{
    public sealed class DiscordIdentityLinkDb
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly CsvDb _db;

        public DiscordIdentityLinkDb(string filePath)
        {
            _db = new CsvDb(filePath, '\t');
        }

        public void Initialize()
        {
            _db.Read();
        }

        public void MakeLink(ulong steamId, ulong discordId)
        {
            if (!TryGetLinkBySteamId(steamId, out var link))
            {
                link = new DiscordIdentityLink();
            }

            link.SteamId = steamId;
            link.DiscordId = discordId;

            var values = SerializeLink(link);
            _db.InsertOrReplace(steamId, values);
            _db.Write();

            Log.Info($"Made link: {link}");
        }

        public bool TryGetLinkBySteamId(ulong steamId, out DiscordIdentityLink link)
        {
            foreach (var values in _db.GetAllValues())
            {
                var lk = DeserializeLink(values);
                if (lk.SteamId == steamId)
                {
                    link = lk;
                    return true;
                }
            }

            link = null;
            return false;
        }

        public bool TryGetLinkByDiscordId(ulong discordId, out DiscordIdentityLink link)
        {
            foreach (var values in _db.GetAllValues())
            {
                var lk = DeserializeLink(values);
                if (lk.DiscordId == discordId)
                {
                    link = lk;
                    return true;
                }
            }

            link = null;
            return false;
        }

        static DiscordIdentityLink DeserializeLink(IReadOnlyList<string> values)
        {
            return new DiscordIdentityLink
            {
                SteamId = ulong.Parse(values[0]),
                DiscordId = ulong.Parse(values[1]),
            };
        }

        static object[] SerializeLink(DiscordIdentityLink link)
        {
            return new object[] {link.SteamId, link.DiscordId};
        }
    }
}