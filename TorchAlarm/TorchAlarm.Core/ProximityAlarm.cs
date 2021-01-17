namespace TorchAlarm.Core
{
    public sealed class ProximityAlarm
    {
        public ProximityAlarm(ulong steamId, string gridName, double distance, OffenderGridInfo offender)
        {
            SteamId = steamId;
            GridName = gridName;
            Distance = distance;
            Offender = offender;
        }

        public ulong SteamId { get; }
        public string GridName { get; }
        public double Distance { get; }
        public OffenderGridInfo Offender { get; }

        public override string ToString()
        {
            return $"{nameof(SteamId)}: {SteamId}, {nameof(GridName)}: {GridName}, {nameof(Distance)}: {Distance}, {nameof(Offender)}: ({Offender})";
        }
    }
}