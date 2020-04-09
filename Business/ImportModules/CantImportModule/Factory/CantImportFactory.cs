using Business.ImportModules.CantImportModule.Imports;
using Common;
using log4net;
using System;
using System.Collections.Generic;

namespace Business.ImportModules.CantImportModule.Factory
{
    public static class CantImportFactory
    {
        private static readonly ILog _log;

        private static IDictionary<CantImportTypeEnum, Type> container;

        static CantImportFactory()
        {
            _log = LogManager.GetLogger(typeof(CantImportFactory));

            container = new Dictionary<CantImportTypeEnum, Type>();
            container.Add(CantImportTypeEnum.Mosaico, typeof(MosaicoImport));
            container.Add(CantImportTypeEnum.Solaris, typeof(SolarisImport));
        }

        public static IImport CreateInstance(CantImportTypeEnum customization, string[] rows, int fil_id = 0)
        {
            if (!container.ContainsKey(customization))
            {
                _log.ErrorFormat("Errore richiesta import non registrato!");
                return null;
            }

            return Activator.CreateInstance(container[customization], new object[] { rows, fil_id }) as IImport;
        }
    }
}
