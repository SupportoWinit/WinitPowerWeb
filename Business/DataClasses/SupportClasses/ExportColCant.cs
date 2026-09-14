using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.SupportClasses
{
    public class ExportColCantDur
    {
        public string descrizioneCant { get; set; }

        public string cognomeCol { get; set; }

        public string nomeCol { get; set; }

        public int durata { get; set; }

        public ExportColCantDur(string descrizioneCant, string cognomeCol, string nomeCol, int durata)
        {
            this.descrizioneCant = descrizioneCant;
            this.cognomeCol = cognomeCol;
            this.nomeCol = nomeCol;
            this.durata = durata;
        }
    }
}
