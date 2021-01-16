namespace TorchAlarm.Core
{
    public sealed class ProximityReport
    {
        public ProximityReport(ulong steamId, long gridId, string gridName, long? offenderFactionId, double distance)
        {
            SteamId = steamId;
            GridId = gridId;
            GridName = gridName;
            OffenderFactionId = offenderFactionId;
            Distance = distance;
        }

        public ulong SteamId { get; }
        public long GridId { get; }
        public string GridName { get; }
        public long? OffenderFactionId { get; }
        public double Distance { get; }
    }
}