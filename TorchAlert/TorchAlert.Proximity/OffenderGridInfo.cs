namespace TorchAlert.Proximity
{
    public sealed class OffenderGridInfo
    {
        public OffenderGridInfo(long gridId, string gridName, string ownerName, long? factionId, string factionTag)
        {
            GridId = gridId;
            FactionId = factionId;
            GridName = gridName;
            OwnerName = ownerName;
            FactionTag = factionTag;
        }

        public long GridId { get; }
        public string GridName { get; }
        public string OwnerName { get; }
        public long? FactionId { get; }
        public string FactionTag { get; }

        public override string ToString()
        {
            return $"{nameof(GridName)}: {GridName}, {nameof(OwnerName)}: {OwnerName ?? "<none>"}, {nameof(FactionId)}: {FactionId ?? 0}, {nameof(FactionTag)}: {FactionTag ?? "<none>"}";
        }
    }
}