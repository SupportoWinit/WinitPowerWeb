using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Resp
    {
      // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella FIL

        public int N_Resp_Utenti
        {
            get
            {
                return Utenti_Resp.Count;
            }
        }  
    }
}
