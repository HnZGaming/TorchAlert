namespace TorchAlert.Proximity
{
    public sealed class ProximityAlert
    {
        public ProximityAlert(ulong steamId, long gridId, string gridName, OffenderGridInfo offender, double distance)
        {
            SteamId = steamId;
            GridId = gridId;
            GridName = gridName;
            Offender = offender;
            Distance = distance;
        }

        public ulong SteamId { get; }
        public long GridId { get; }
        public string GridName { get; }
        public OffenderGridInfo Offender { get; }
        public double Distance { get; }

        public override string ToString()
        {
            return $"{nameof(SteamId)}: {SteamId}, {nameof(GridName)}: {GridName}, {nameof(Distance)}: {Distance}, {nameof(Offender)}: ({Offender})";
        }
    }
}