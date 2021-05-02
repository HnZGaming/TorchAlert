using System;
using System.Collections.Generic;
using System.Linq;
using Utils.General;
using VRageMath;

namespace TorchAlert.Proximity
{
    public readonly struct DefenderGridInfo
    {
        public DefenderGridInfo(
            long gridId,
            string gridName,
            long factionId,
            string factionName,
            Vector3D position,
            IEnumerable<ulong> steamIds)
        {
            FactionName = factionName;
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

        public readonly long GridId;
        public readonly string GridName;
        public readonly long FactionId;
        public readonly string FactionName;
        public readonly Vector3D Position;
        public readonly IEnumerable<ulong> SteamIds;

        public override string ToString()
        {
            return $"\"{GridName}\" <{GridId}> [{FactionName}] {SteamIds.ToStringSeq()}";
        }
    }
}