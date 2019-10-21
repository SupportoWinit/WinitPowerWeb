using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ImportModules
{
    public interface IImport
    {
        IDictionary<string, string> Import();
    }
}
