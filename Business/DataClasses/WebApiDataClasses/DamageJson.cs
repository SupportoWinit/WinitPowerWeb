using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.WebApiDataClasses
{
    public class DamageJson
    {
        public string Id { get; set; }
        public string BadgeCode { get; set; }
        public string DeviceCode { get; set; }
        public DateTime DamageDateTime { get; set; }
        public int CantCode { get; set; }
        public int ColCode { get; set; }
        public int DamageSubCantId { get; set; }
        public string DescriptionDamageType { get; set; } //Macro descrizione del tipo di segnalazione
        public string DescriptionDamageSubType { get; set; } //Micro descrizione del tipo di segnalazione
        public bool DamageHasAudio { get; set; } 
        public bool DamageHasImage { get; set; }
        public string MultimediaName { get; set; }
        public string DamageNote { get; set; }
        public Common.JsonSegnalazioneTypeEnum Type { get; set; }
    }
}
 