using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.IocFactory.ClockAppSynchronizationFactory
{
    public interface ISynchronizationProcessor<T> where T : class
    {
        void Synchronize(IEnumerable<DbEntityEntry<T>> addedEntities, IEnumerable<Dictionary<string,object>> modifiedEntities, IEnumerable<DbEntityEntry<T>> deletedEntities);
    }
}
