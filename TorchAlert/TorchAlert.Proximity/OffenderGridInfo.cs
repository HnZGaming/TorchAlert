namespace TorchAlert.Proximity
{
    public readonly struct OffenderGridInfo
    {
        public OffenderGridInfo(long gridId, string gridName, string ownerName, long factionId, string factionTag)
        {
            GridId = gridId;
            FactionId = factionId;
            GridName = gridName;
            OwnerName = ownerName;
            FactionTag = factionTag;
        }

        public readonly long GridId;
        public readonly string GridName;
        public readonly string OwnerName;
        public readonly long FactionId;
        public readonly string FactionTag;

        public override string ToString()
        {
            return $"\"{GridName}\" <{GridId}> [{FactionTag}] \"{OwnerName}\"";
        }
    }
}