using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Fru_Cant
    {
      // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella CANT

        public string Codice_Cantiere
        {
            get
            {
                if (Cant != null)
                    return Cant.Codice_Cantiere;
                else
                    return null;
            }
        }

        public string Descrizione_Can
        {
            get
            {
                if (Cant != null)
                    return Cant.Descrizione_Can;
                else
                    return null;
            }
        }

        public string Codice_Fru
        {
            get
            {
                if (Fru != null)
                    return Fru.Codice_Fru;
                else
                    return null;
            }
        }
        public string N_Serie_Fru
        {
          get
          {
            if (Fru != null)
              return Fru.N_Serie_Fru;
            else
              return null;
          }
        }

        public static string ClockAppApiPath
        {
            get
            {
                return "//api/AddCantsFromPowerWeb/";
            }
        }

    }      
}
