using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace TorchAlarm.Core
{
    // must be testable
    public sealed class ProximityScanner
    {
        public interface IConfig
        {
            int ProximityThreshold { get; }
        }

        readonly IConfig _config;

        public ProximityScanner(IConfig config)
        {
            _config = config;
        }

        public IEnumerable<Proximity> ScanProximity(IEnumerable<GridInfo> grids)
        {
            if (grids.Take(3).Count() < 3)
            {
                return ScanProximityInternal(grids);
            }

            // get the bound for octree
            var positions = grids.Select(g => g.Position);
            var box = BoundingBoxD.CreateFromPoints(positions);
            var boxSize = box.Size;
            var biggestLength = Math.Max(Math.Max(boxSize.X, boxSize.Y), boxSize.Z);
            box.InflateToMinimum(biggestLength); // same length in all axes
            var boxLength = box.Size.X;

            var minSectionLength = _config.ProximityThreshold * 5;
            var depth = GetOctreeDepth(boxLength, minSectionLength);
            var octree = new Octree<GridInfo>(depth);
            foreach (var grid in grids)
            {
                octree.Add(grid.Position, grid);
            }

            return octree
                .GetLeaves()
                .SelectMany(s => ScanProximityInternal(s));
        }

        IEnumerable<Proximity> ScanProximityInternal(IEnumerable<GridInfo> grids)
        {
            foreach (var g0 in grids)
            foreach (var g1 in grids)
            {
                if (g0.GridId == g1.GridId) continue;

                var distance = Vector3D.Distance(g0.Position, g1.Position);
                if (distance < _config.ProximityThreshold)
                {
                    yield return new Proximity(g0, g1, distance);
                }
            }
        }

        static int GetOctreeDepth(double rootLength, double minLength)
        {
            var currentLength = rootLength;
            var depth = 0;
            while (currentLength > minLength)
            {
                currentLength /= 2;
                depth += 1;
            }

            return depth;
        }
    }
}