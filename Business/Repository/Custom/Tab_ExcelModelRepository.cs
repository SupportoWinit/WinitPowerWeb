using Data;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.Custom
{
    public class Tab_Excel_ModelRepository : GenericRepository<Tab_Excel_Model>, ITab_Excel_ModelRepository
    {
        public Tab_Excel_ModelRepository(PowerWebEntities context)
            : base(context)
        {

        }

        public IEnumerable<Tab_Excel_Model> GetActiveTab_Excel_ModelsByPage(string page)
        {
            var tab_ExcelModels = Find(tem => tem.IsActive && tem.Pagina == page, true).ToList();

            tab_ExcelModels.ForEach(p =>
            {
                p.Nome_Risorsa = BusinessService.GetLocalizedString(p.Nome_Risorsa);
            });

            return tab_ExcelModels;

        }

    }
}
