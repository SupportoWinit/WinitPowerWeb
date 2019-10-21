using Business.BusinessServices.ImportCustomerService.CantCustomerImport;
using Business.BusinessServices.ImportCustomerService.Interfaces;
using Business.Repository;
using Common;
using log4net;
using System;
using System.Collections.Generic;

namespace Business.BusinessServices.ImportCustomerService.Factory
{
    public class CustomerImportFactory
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CustomerImportFactory));

        private IDictionary<CantImportTypeEnum, Type> _cantRegister;
        private IDictionary<ColImportTypeEnum, Type> _colRegister;
        
        public CustomerImportFactory()
        {

            _cantRegister = new Dictionary<CantImportTypeEnum, Type>();
            _colRegister = new Dictionary<ColImportTypeEnum, Type>();

            #region Registrazione import cantieri

            _cantRegister.Add(CantImportTypeEnum.GeneraleCantiere, typeof(DefaultImport));
            _cantRegister.Add(CantImportTypeEnum.Mosaico, typeof(MosaicoImport));

            #endregion

            #region Registrazione import collaboratori


            #endregion

            //_colRegister.Add(ColImportTypeEnum.Generale)

        }


        /// <summary>
        /// Viene creata un'instanza di import cantieri seguendo la 
        /// personalizzazione corrente.
        /// </summary>
        /// <returns></returns>
        public ICustomerImport CreateCantImportInstance()
        {
            CantImportTypeEnum cantImportConfig = (CantImportTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantImportTypeEnum);

            if (!_cantRegister.ContainsKey(cantImportConfig))
            {
                _log.ErrorFormat("L'import cantieri selezionato ({0}) non è registrato tra le personalizzazioni", cantImportConfig);

                return null;
            }

            Type cantImportType = _cantRegister[cantImportConfig];

            ICustomerImport import = Activator.CreateInstance(cantImportType) as ICustomerImport;

            return import;

        }

        /// <summary>
        /// Viene creata un'instanza di import cantieri seguendo la 
        /// personalizzazione corrente.
        /// </summary>
        /// <returns></returns>
        public ICustomerImport CreateColImportInstance()
        {
            ColImportTypeEnum colImportConfig = (ColImportTypeEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ColImportTypeEnum);

            if (!_colRegister.ContainsKey(colImportConfig))
            {
                _log.ErrorFormat("L'import collaboratori selezionato ({0}) non è registrato tra le personalizzazioni", colImportConfig);

                return null;
            }

            Type colImportType = _colRegister[colImportConfig];

            ICustomerImport import = Activator.CreateInstance(colImportType) as ICustomerImport;

            return import;

        }
    }
}
