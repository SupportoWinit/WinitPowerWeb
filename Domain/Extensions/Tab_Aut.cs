using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Domain
{
    public partial class Tab_Aut
    {
        public String Codice_Utente_Aut
        {
            get
            {
                if (Utenti != null)
                {
                    return Utenti.Codice_Utente;
                }
                else
                {
                    return null;
                }
            }
        }

        public String Nome_Tab_Funz
        {
            get;
            set;
        }
    }
}
