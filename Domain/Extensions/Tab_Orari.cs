using System;
using Common;

namespace Domain
{
    public partial class Tab_Orari
    {
        // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella TAB_ORARI    
        public string Tab_Orari_Tipo_Desc
        {
            get
            {
                if (Tab_Orari_Tipo != null)                    
                    return Tab_Orari_Tipo.Tab_Orari_Tipo_Desc;
                else
                    return null;
            }
            
        }

        public string Cant_Desc
        {
            get
            {
                return "";
            }
        }

        public TimeSpan DisplayedDuration
        {
            get
            {
                return CommonService.GetTimeSpanFromMinutes(Durata_Minuti);
            }

            set
            {
                Durata_Minuti = CommonService.GetMinutesFromTimeSpan(value);
            }
        }
    }
}
