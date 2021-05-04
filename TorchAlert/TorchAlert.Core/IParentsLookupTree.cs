using System.Collections.Generic;

namespace TorchAlert.Core
{
    public interface IParentsLookupTree<T>
    {
        IEnumerable<T> GetParentsOf(T child);
    }
}