namespace TorchAlarm.Core
{
    public sealed class OffenderGridInfo
    {
        public OffenderGridInfo(long gridId, string gridName, string ownerName, long? factionId, string factionTag, string factionName)
        {
            GridId = gridId;
            FactionId = factionId;
            GridName = gridName;
            OwnerName = ownerName;
            FactionName = factionName;
            FactionTag = factionTag;
        }

        public long GridId { get; }
        public string GridName { get; }
        public string OwnerName { get; }
        public long? FactionId { get; }
        public string FactionName { get; }
        public string FactionTag { get; }

        public override string ToString()
        {
            return $"{nameof(GridName)}: {GridName}, {nameof(OwnerName)}: {OwnerName ?? "<none>"}, {nameof(FactionId)}: {FactionId ?? 0}, {nameof(FactionName)}: {FactionName ?? "<none>"}, {nameof(FactionTag)}: {FactionTag ?? "<none>"}";
        }
    }
}