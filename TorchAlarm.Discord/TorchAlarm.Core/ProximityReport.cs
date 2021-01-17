namespace TorchAlarm.Core
{
    public sealed class ProximityReport
    {
        public ProximityReport(ulong steamId, string gridName, OffenderGridInfo offender, double distance)
        {
            SteamId = steamId;
            GridName = gridName;
            Offender = offender;
            Distance = distance;
        }

        public ulong SteamId { get; }
        public string GridName { get; }
        public OffenderGridInfo Offender { get; }
        public double Distance { get; }
    }
}