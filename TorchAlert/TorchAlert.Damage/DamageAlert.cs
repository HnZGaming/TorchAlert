namespace TorchAlert.Damage
{
    public sealed class DamageAlert
    {
        public DamageAlert(ulong steamId, string gridName, string offenderName, string offenderFactionName, string offenderFactionTag)
        {
            SteamId = steamId;
            OffenderName = offenderName;
            OffenderFactionName = offenderFactionName;
            OffenderFactionTag = offenderFactionTag;
        }

        public ulong SteamId { get; }
        public string GridName { get; }
        public string OffenderName { get; }
        public string OffenderFactionName { get; }
        public string OffenderFactionTag { get; }
    }
}