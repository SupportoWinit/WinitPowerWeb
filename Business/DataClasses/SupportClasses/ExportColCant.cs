using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.SupportClasses
{
    public class ExportColCant
    {
        public string descrizioneCant { get; set; }

        public string descrizioneCol { get; set; }

        public string descrizioneAtt { get; set; }

        public TimeSpan durata { get; set; }

        public ExportColCant(string descrizioneCant, string descrizioneCol, string descrizioneAtt, TimeSpan durata)
        {
            this.descrizioneCant = descrizioneCant;
            this.descrizioneCol = descrizioneCol;
            this.descrizioneAtt = descrizioneAtt;
            this.durata = durata;
        }
    }
}
