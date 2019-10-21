using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Cli
    {        
        // CAMPI AGGIUNTIVI (calcolati) della Tabella CLI         

        public string CognomeNome_Cli
        {
          get { return new StringBuilder(Cognome_Cli).Append(" ").Append(Nome_Cli).ToString(); }
        }       
        
    }
}
