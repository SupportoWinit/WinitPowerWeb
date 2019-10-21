using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
  public partial class Utenti_Fil
  {
    // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella UTENTI_FIL

    
    public string Descrizione_Fil
    {
      get
      {
        if (Fil != null)
          return Fil.Descrizione_Fil;
        else
          return null;
      }
    }
    
  }
}
