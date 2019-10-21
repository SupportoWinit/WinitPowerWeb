using Business.ClockAppManagerSynchronizationUtilities;
using Business.IocFactory.ClockAppSynchronizationFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository
{
    public interface ISynchronizationCollector<T> where T : class
    {
        SynchronizationCollector<T> SynchronizationHub { get; set; }
    }
}
