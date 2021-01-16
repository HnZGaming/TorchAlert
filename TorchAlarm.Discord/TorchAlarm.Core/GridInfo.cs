using VRageMath;

namespace TorchAlarm.Core
{
    public sealed class GridInfo
    {
        public GridInfo(
            long gridId,
            string gridName,
            long? factionId,
            Vector3D position,
            bool isStatic)
        {
            IsStatic = isStatic;
            GridName = gridName;
            FactionId = factionId;
            Position = position;
            GridId = gridId;
        }

        public long GridId { get; }
        public string GridName { get; }
        public long? FactionId { get; }
        public Vector3D Position { get; }
        public bool IsStatic { get; }
    }
}