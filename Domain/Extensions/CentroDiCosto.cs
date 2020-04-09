using System.Collections.Generic;

namespace Domain
{
    public partial class CentroDiCosto
    {
        public override bool Equals(object obj)
        {
            return obj is CentroDiCosto costo &&
                   Codice == costo.Codice &&
                   Descrizione == costo.Descrizione;
        }

        public override int GetHashCode()
        {
            var hashCode = 1308125260;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Codice);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Descrizione);
            return hashCode;
        }
    }
}
