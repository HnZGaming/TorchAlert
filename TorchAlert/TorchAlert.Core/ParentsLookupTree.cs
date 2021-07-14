using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TorchAlert.Core
{
    public sealed class ParentsLookupTree<T> : IParentsLookupTree<T>
    {
        sealed class Node
        {
            public Node(T value)
            {
                Value = value;
            }

            T Value { get; }
            public Node Parent { private get; set; }

            public IEnumerable<T> TraverseParents()
            {
                return TraverseParents(Parent);
            }

            static IEnumerable<T> TraverseParents(Node node)
            {
                while (node != null)
                {
                    yield return node.Value;
                    node = node.Parent;
                }
            }
        }

        readonly Dictionary<T, Node> _nodes;

        public ParentsLookupTree()
        {
            _nodes = new Dictionary<T, Node>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(T parent, T child)
        {
            if (!_nodes.TryGetValue(parent, out var parentNode))
            {
                parentNode = new Node(parent);
                _nodes[parent] = parentNode;
            }

            var childNode = new Node(child);
            _nodes[child] = childNode;

            childNode.Parent = parentNode;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<T> GetParentsOf(T child)
        {
            if (!_nodes.TryGetValue(child, out var childNode))
            {
                return Enumerable.Empty<T>();
            }

            return childNode.TraverseParents();
        }
    }
}