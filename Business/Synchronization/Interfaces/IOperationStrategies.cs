using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Business.Synchronization
{
    public interface IOperationStrategies<T> where T : class
    {
        JArray ExecuteAddStrategy(IEnumerable<T> entities);

        JArray ExecuteModifyStrategy(IEnumerable<T> entities);

        JArray ExecuteDeletionStrategy(IEnumerable<T> entities);
    }
}
