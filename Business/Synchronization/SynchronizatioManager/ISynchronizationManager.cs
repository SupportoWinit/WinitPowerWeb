using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Synchronization.SynchronizatioManager
{
    public interface ISynchronizationManager<T> where T : class
    {
        void Collect();

        void SynchronizeEntities();
    }
}
