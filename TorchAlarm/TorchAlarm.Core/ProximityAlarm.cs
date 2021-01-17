namespace TorchAlarm.Core
{
    public sealed class ProximityAlarm
    {
        public ProximityAlarm(ulong steamId, long gridId, string gridName, double distance, OffenderGridInfo offender)
        {
            SteamId = steamId;
            GridId = gridId;
            GridName = gridName;
            Distance = distance;
            Offender = offender;
        }

        public ulong SteamId { get; }
        public long GridId { get; }
        public string GridName { get; }
        public double Distance { get; }
        public OffenderGridInfo Offender { get; }

        public override string ToString()
        {
            return $"{nameof(SteamId)}: {SteamId}, {nameof(GridName)}: {GridName}, {nameof(Distance)}: {Distance}, {nameof(Offender)}: ({Offender})";
        }
    }
}