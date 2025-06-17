using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.SupportClasses
{
    public class ExportColCantInt
    {
        public string descrizioneCant { get; set; }

        public string descrizioneCol { get; set; }

        public string descrizioneAtt { get; set; }

        public int durata { get; set; }

        public int interventi { get; set; }

        public ExportColCantInt(string descrizioneCant, string descrizioneCol, string descrizioneAtt, int durata, int interventi)
        {
            this.descrizioneCant = descrizioneCant;
            this.descrizioneCol = descrizioneCol;
            this.descrizioneAtt = descrizioneAtt;
            this.durata = durata;
            this.interventi = interventi;
        }
    }
}
