using Common;
using log4net;
using System;
using System.Collections.Generic;

namespace Business.ImportModules.ColImportModule.Factory
{
    public static class ColImportFactory
    {
        private static readonly ILog _log;

        private static IDictionary<int, Type> container;

        static ColImportFactory()
        {
            _log = LogManager.GetLogger(typeof(ColImportFactory));

            container = new Dictionary<int, Type>();
        }

        public static IImport CreateInstance(int customization)
        {
            if (!container.ContainsKey(customization))
            {
                _log.ErrorFormat("Errore richiesta import non registrato!");
                return null;
            }

            return Activator.CreateInstance(container[customization], new object[] { LogManager.GetLogger(container[customization]) }) as IImport;
        }
    }
}


