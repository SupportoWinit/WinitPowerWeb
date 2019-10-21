using System.Collections.Generic;
using System.Data.Entity.Infrastructure;

namespace Business.Synchronization
{
    public interface ICollector<T> where T :class
    {
        IEnumerable<T> GetAddedEntities();

        IEnumerable<T> GetModifiedEntities();

        IEnumerable<T> GetDeletedEntities();

        void Collect();
    }
}
