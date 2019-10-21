using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public partial class Tab_Orari_Tipo
    {
        public DateTime? FirstTabOrariTipo {

            get {

                if (Tab_Orari.Count > 0)
                    return Tab_Orari.OrderBy(to => to.Data_Inizio).First().Data_Inizio;
                return null;
            }
        }

        
    }
}
