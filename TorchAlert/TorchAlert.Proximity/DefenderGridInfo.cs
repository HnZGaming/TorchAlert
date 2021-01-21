﻿using System;
using System.Collections.Generic;
using System.Linq;
using Utils.General;
using VRageMath;

namespace TorchAlert.Proximity
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

        public override string ToString()
        {
            return $"{nameof(GridName)}: {GridName}, {nameof(SteamIds)}: {SteamIds.ToStringSeq()}, {nameof(FactionId)}: {FactionId ?? 0}";
        }
    }
}