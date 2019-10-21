using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public partial class Pru
    {
        public int N_Pru_Col
        {
            get
            {
                return Pru_Col.Count;
            }
        }
        public List<Pru_Col> RPru_Cols
        {
            get
            {
                return Pru_Col.OrderByDescending(pruCol => pruCol.Abilitazione_Data_Inizio_Pru_Col).ToList();
            }
        }
    }
}
