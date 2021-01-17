using System;
using SQLite;

namespace TorchAlarm.Discord
{
    public sealed class DiscordIdentityLink
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public ulong SteamId { get; set; }

        [Indexed]
        public ulong DiscordId { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(SteamId)}: {SteamId}, {nameof(DiscordId)}: {DiscordId}";
        }
    }
}