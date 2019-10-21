using Business.Synchronization.Collectors;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Synchronization.Synchronizators
{
    public class Synchronizator<T> : ISynchronizator<T> where T : class
    {

        public ICollector<T> Collector { get; set; }

        public ISynchronizer<T> Synchronizer { get; set; }

        public Synchronizator(ISynchronizer<T> synchronizer, IOperationStrategies<T> operations)
        {
            Synchronizer = synchronizer;
            Synchronizer.Strategies = operations;
        }



        public void Synchronize()
        {
            Synchronizer.Synchronize();
        }

        public void Collect(DbContext context)
        {
            Collector = new DbChangeTrackerCollector<T>(context.ChangeTracker);
            Synchronizer.Collector = Collector;
            
            Collector.Collect();
        }
    }
}
