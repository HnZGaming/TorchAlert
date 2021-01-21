namespace TorchAlert.Damage
{
    public sealed class DamageAlert
    {
        public DamageAlert(ulong steamId, string gridName, string offenderName, string offenderFactionTag)
        {
            SteamId = steamId;
            GridName = gridName;
            OffenderName = offenderName;
            OffenderFactionTag = offenderFactionTag;
        }

        public ulong SteamId { get; }
        public string GridName { get; }
        public string OffenderName { get; }
        public string OffenderFactionTag { get; }

        public override string ToString()
        {
            return $"{nameof(SteamId)}: {SteamId}, {nameof(GridName)}: {GridName}, {nameof(OffenderName)}: {OffenderName}, {nameof(OffenderFactionTag)}: {OffenderFactionTag}";
        }
    }
}