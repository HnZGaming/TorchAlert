namespace TorchAlert.Core.Proximity
{
    public readonly struct ProximityAlert
    {
        public ProximityAlert(ulong steamId, long defenderGridId, string defenderGridName, OffenderGridInfo offender, double distance)
        {
            SteamId = steamId;
            DefenderGridId = defenderGridId;
            DefenderGridName = defenderGridName;
            Offender = offender;
            Distance = distance;
        }

        public readonly ulong SteamId;
        public readonly long DefenderGridId;
        public readonly string DefenderGridName;
        public readonly OffenderGridInfo Offender;
        public readonly double Distance;

        public override string ToString()
        {
            return $"\"{DefenderGridName} ({DefenderGridName})\" <{SteamId}> offended by: {{{Offender}}}, Distance: {Distance}";
        }
    }
}