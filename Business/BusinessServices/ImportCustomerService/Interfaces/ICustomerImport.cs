using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ImportCustomerService.Interfaces
{
    public interface ICustomerImport
    {

        IEnumerable<KeyValuePair<string, string>> Import(IEnumerable<string> lines);

    }
}
