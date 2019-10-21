using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace Domain
{
    public partial class Tab_Comuni
    {
      public string Sigla_Prov
      {
        get { return Tab_Prov  != null ? Tab_Prov.Sigla_Prov : null; }
      }  
    }
}
