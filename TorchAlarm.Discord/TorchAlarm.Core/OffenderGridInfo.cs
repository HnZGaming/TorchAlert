namespace TorchAlarm.Core
{
    public sealed class OffenderGridInfo
    {
        public OffenderGridInfo(long gridId, long? factionId)
        {
            GridId = gridId;
            FactionId = factionId;
        }

        public long GridId { get; }
        public long? FactionId { get; }
    }
}