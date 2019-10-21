using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.Custom
{
    public interface ITab_Excel_ModelRepository : IRepository<Tab_Excel_Model>
    {
        IEnumerable<Tab_Excel_Model> GetActiveTab_Excel_ModelsByPage(string page);

    }
}
