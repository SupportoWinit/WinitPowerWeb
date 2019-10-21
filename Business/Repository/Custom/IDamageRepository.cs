using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.Custom
{
    public interface IDamageRepository : IRepository<Damage>
    {


        List<Damage> ImportRowSegnalazioni(IEnumerable<string> rowSegnalazioni, List<KeyValuePair<string,string>> importErrors);

        void ElaborateRowSegnalazioni(DateTime from, DateTime to);



    }
}
