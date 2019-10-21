using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Pru_Col
    {
      // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella PRU_COL

        public string Codice_Col
        {
            get
            {
                if (Col != null)
                    return Col.Codice_Collaboratore;
                else
                    return null;
            }
        }

        public string Nome_Col
        {
            get
            {
                if (Col != null)
                    return Col.Nome_Col;
                else
                    return null;
            }
        }

        public string Cognome_Col
        {
            get
            {
                if (Col != null)
                    return Col.Cognome_Col;
                else
                    return null;
            }
        }

        public string CognomeNome_Col
        {
            get
            {
                if (Col != null)
                    return String.Format("{0} {1}", Col.Cognome_Col, Col.Nome_Col);
                else
                    return null;
            }
        }
        public string N_Serie_Pru
        {
            get
            {
                if (Pru != null)
                    return Pru.N_Serie_Pru;
                else
                    return null;
            }
        }

        public string Codice_Pru
        {
            get
            {
                if (Pru != null)
                    return Pru.Codice_Pru;
                else
                    return null;
            }
        }
        
    }
}
