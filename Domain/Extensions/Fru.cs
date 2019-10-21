using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Fru
    {
      // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella COL
      
      public int N_Fru_Cant     
      {
        get { return Fru_Cant.Count; }
      }
            
      // LISTE CAMPI per rendere visibili le Tabelle anche nella FIELD LIST degli XRREPORT
      
      public List<Fru_Cant> RFru_Cants      
      {
        get { return Fru_Cant.OrderByDescending(fruCant => fruCant.Abilitazione_Data_Inizio_Fru_Can).ToList(); }
      }      
    }
}
