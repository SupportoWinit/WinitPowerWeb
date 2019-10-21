namespace Business.Synchronization
{
    public interface ISynchronizer<T> where T : class
    {
        ICollector<T> Collector { get; set; }

        IOperationStrategies<T> Strategies { get; set; }

        void Synchronize();
    }
}
