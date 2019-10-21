using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Col_V
    {
        // CAMPI AGGIUNTIVI E/O CALCOLATI della Vista COL_V

        public string CognomeNome_Col
        {
            get { return new StringBuilder(Cognome_Col).Append(" ").Append(Nome_Col).ToString(); }
        }
    }
}
