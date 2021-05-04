namespace TorchAlert.Core.Proximity
{
    public readonly struct ProximityAlert
    {
        public ProximityAlert(ulong steamId, long gridId, string gridName, OffenderGridInfo offender, double distance)
        {
            SteamId = steamId;
            GridId = gridId;
            GridName = gridName;
            Offender = offender;
            Distance = distance;
        }

        public readonly ulong SteamId;
        public readonly long GridId;
        public readonly string GridName;
        public readonly OffenderGridInfo Offender;
        public readonly double Distance;

        public override string ToString()
        {
            return $"\"{GridName} ({GridName})\" <{SteamId}>, Offender: {{{Offender}}}, Distance: {Distance}";
        }
    }
}