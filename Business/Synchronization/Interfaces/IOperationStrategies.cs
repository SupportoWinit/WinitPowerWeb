using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Business.Synchronization
{
    public interface IOperationStrategies<T> where T : class
    {
        JArray ExecuteAddStrategy(IEnumerable<T> entities);

        JArray ExecuteAddStrategy(IEnumerable<T> entities, IDictionary<string, string> connectionString);

        JArray ExecuteModifyStrategy(IEnumerable<T> entities);

        JArray ExecuteModifyStrategy(IEnumerable<T> entities, IDictionary<string, string> connectionString);

        JArray ExecuteDeletionStrategy(IEnumerable<T> entities);

        JArray ExecuteDeleteStrategy(IEnumerable<T> entities, IDictionary<string, string> connectionString);
    }
}
