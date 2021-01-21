namespace Discord.Torch
{
    public sealed class DiscordIdentityLink
    {
        public ulong SteamId { get; set; }
        public ulong DiscordId { get; set; }

        public override string ToString()
        {
            return $"{nameof(SteamId)}: {SteamId}, {nameof(DiscordId)}: {DiscordId}";
        }
    }
}