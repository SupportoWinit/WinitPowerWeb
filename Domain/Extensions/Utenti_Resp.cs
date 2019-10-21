using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
  public partial class Utenti_Resp
  {
    // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella UTENTI_RESP

    
    public string Descrizione_Resp
    {
      get
      {
        if (Resp != null)
          return Resp.Descrizione_Resp;
        else
          return null;
      }
    }
    
  }
}
