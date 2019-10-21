using System;

namespace Domain
{
    public partial class Cant_V
    {

        public string Descrizione_Fil
        {
            get { return this.Fil_Desc; }
        }

        public string Fil_Cod
        {
            get { if (Fil_Id.HasValue) return this.Fil_Cod; else return String.Empty; }
        }

    }
}
