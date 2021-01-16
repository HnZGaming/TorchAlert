using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace TorchAlarm.Core
{
    public sealed class Octree<T> : IEnumerable<T>
    {
        readonly int _depth;
        readonly Octree<T>[] _children;
        readonly List<T> _elements;

        public Octree(int depth)
        {
            _depth = depth;
            if (_depth == 0)
            {
                _elements = new List<T>();
            }
            else
            {
                _children = new Octree<T>[8];
                for (var i = 0; i < 8; i++)
                {
                    _children[i] = new Octree<T>(_depth - 1);
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _depth == 0
                ? _elements.GetEnumerator()
                : _children.SelectMany(c => c).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<IEnumerable<T>> GetLeaves()
        {
            switch (_depth)
            {
                case 0: return new[] {_elements};
                case 1: return _children;
                default: return _children.SelectMany(c => c.GetLeaves());
            }
        }

        public void Add(Vector3D normalPosition, T element)
        {
            if (_depth == 0)
            {
                _elements.Add(element);
                return;
            }

            var (x, nestedPositionX) = GoDeep(normalPosition.X);
            var (y, nestedPositionY) = GoDeep(normalPosition.Y);
            var (z, nestedPositionZ) = GoDeep(normalPosition.Z);
            var i = x + y * 2 + z * 4;

            var deepNormalPosition = new Vector3D(
                x: nestedPositionX,
                y: nestedPositionY,
                z: nestedPositionZ);

            _children[i].Add(deepNormalPosition, element);
        }

        static (int, double) GoDeep(double normal)
        {
            var index = (int) Math.Max(Math.Round(normal * 2), 1);
            var nestedPosition = index == 0 ? normal * 2 : (normal - 0.5d) * 2;
            return (index, nestedPosition);
        }
    }
}