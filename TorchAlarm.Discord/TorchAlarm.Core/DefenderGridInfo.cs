using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace TorchAlarm.Core
{
    public sealed class DefenderGridInfo
    {
        public DefenderGridInfo(
            long gridId,
            string gridName,
            long? factionId,
            Vector3D position,
            IEnumerable<ulong> steamIds)
        {
            GridId = gridId;
            GridName = gridName;
            FactionId = factionId;
            Position = position;
            SteamIds = steamIds;

            if (!SteamIds.Any())
            {
                throw new Exception($"no receivers: \"{gridName}\"");
            }
        }

        public long GridId { get; }
        public string GridName { get; }
        public long? FactionId { get; }
        public Vector3D Position { get; }
        public IEnumerable<ulong> SteamIds { get; }
    }
}