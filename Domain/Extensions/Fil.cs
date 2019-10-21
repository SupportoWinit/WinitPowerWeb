using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Fil
    {
      // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella FIL

        public int N_Fil_Utenti
        {
            get
            {
                return Utenti_Fil.Count;
            }
        }
    }
}
