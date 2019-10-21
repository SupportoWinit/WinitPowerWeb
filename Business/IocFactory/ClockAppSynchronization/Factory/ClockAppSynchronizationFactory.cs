using Business.ClockAppManagerSynchronizationUtilities.SynchronizationProcessors;
using Business.ClockAppManagerSynchronizationUtilities.SynchronizationProcessors.Synchronizers;
using Business.IocFactory.ClockAppSynchronizationFactory.ClockAppSynchronizators;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.IocFactory.ClockAppSynchronizationFactory
{
    public static class ClockAppSynchronizationFactory
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ClockAppSynchronizationFactory));

        private static IDictionary<Type,Type> container;

        static ClockAppSynchronizationFactory()
        {
            container = new Dictionary<Type,Type>();

            container.Add(typeof(Cant), typeof(CantSynchronizer));
            container.Add(typeof(Col), typeof(ColSynchronizer));
            container.Add(typeof(Fru_Cant), typeof(FruCantSynchronizer));
            container.Add(typeof(Pru_Col), typeof(PruColSynchronizer));
            container.Add(typeof(Tab_Damage), typeof(Tab_DamageSynchronizer));
            
        }

        public static ISynchronizationProcessor<T> CreateInstance<T>() where T : class
        {
            if (!container.ContainsKey(typeof(T)))
            {
                _log.ErrorFormat("Errore causato da richiesta sincronizzazione per entità non registrata {0}", typeof(T).Name);
                return null;
            }

            Type requestedType = container[typeof(T)];

            return Activator.CreateInstance(requestedType) as ISynchronizationProcessor<T>;
        }
        
    }
}
