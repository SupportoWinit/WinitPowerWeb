using Business.BusinessServices.ImportCustomerService.Factory;
using log4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Business.BusinessServices.ImportCustomersService
{
    public class ImportCustomersService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ImportCustomersService));

        private ICollection<KeyValuePair<string, string>> _errors;
        private CustomerImportFactory _factory;

        public IEnumerable<KeyValuePair<string, string>> Errors
        {
            get
            {
                return _errors;
            }
        }

        #region Costruttori

        public ImportCustomersService() : this(new CustomerImportFactory()) { }

        public ImportCustomersService(CustomerImportFactory factory)
        {
            _factory = factory;

            _errors = new List<KeyValuePair<string, string>>();
        }

        #endregion
        public IEnumerable<KeyValuePair<string, string>> ImportCantsFromCsv(string path)
        {

            var lines = ParseFromCsv(path);
            var importInstance = _factory.CreateCantImportInstance();
            var errors = importInstance.Import(lines);

            return errors;
        }
        public IEnumerable<KeyValuePair<string, string>> ImportColsFromCsv(string path)
        {
            var lines = ParseFromCsv(path);
            var importInstance = _factory.CreateColImportInstance();
            var errors = importInstance.Import(lines);

            return errors;
        }

        private IEnumerable<string> ParseFromCsv(string path)
        {
            return File.ReadAllText(path, Encoding.GetEncoding(850)).Split('\r').Where(line => !line.All(c => c == ';'));
        }









    }
}
