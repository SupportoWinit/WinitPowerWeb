using System.Data.Entity;

namespace Business.Synchronization
{
    public interface ISynchronizator<T> where T : class
    {
        ICollector<T> Collector { get; set; }

        ISynchronizer<T> Synchronizer { get; set; }

        void Collect(DbContext context);

        void Synchronize();
    }
}
