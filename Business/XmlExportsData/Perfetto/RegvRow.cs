using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Business.XmlExportsData.Perfetto
{
    /// <summary>
    /// Classe che rappresenta una riga da estrarre verso Perfetto
    /// </summary>
    public class RegvRow
    {

        #region Public Properties

        /// <summary>
        /// Recupera o imposta l'ora di inzio della timbratura (in secondi).
        /// </summary>
        /// <value>
        /// L'ora di inzio della timbratura (in secondi).
        /// </value>
        public int StartHour { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di fine della timbratura (in secondi).
        /// </summary>
        /// <value>
        /// L'ora di fine della timbratura (in secondi).
        /// </value>
        public int EndHour { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del collaboratore di riferimento per la timbratura.
        /// </summary>
        /// <value>
        /// L'id del collaboratore di riferimento per la timbratura.
        /// </value>
        public int ColId { get; set; }

        /// <summary>
        /// Recupera o imposta il codice del collaboratore di riferimento per la timbratura.
        /// </summary>
        /// <value>
        /// Il codice del collaboratore di riferimento per la timbratura.
        /// </value>
        public string ColMnemonic { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del cantiere di riferimento per timbratura.
        /// </summary>
        /// <value>
        /// L'id del cantiere di riferimento per timbratura.
        /// </value>
        public int CantId { get; set; }

        /// <summary>
        /// Recupera o imposta il codice del cantiere di riferimento per la timbratura.
        /// </summary>
        /// <value>
        /// Il codice del cantiere di riferimento per la timbratura.
        /// </value>
        public string CantMnemonic { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore ordinarie (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore ordinarie (in secondi).
        /// </value>
        public int OrdinaryHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore straordinarie (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore straordinarie (in secondi).
        /// </value>
        public int OvertimeHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore di viaggio (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore di viaggio (in secondi).
        /// </value>
        public int TravelHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore viaggio comprese nelle 8.
        /// </summary>
        /// <value>
        /// Il numero di ore viaggio comprese nelle 8.
        /// </value>
        public int Travel8hHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore di ferie/permessi (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore di ferie/permessi (in secondi).
        /// </value>
        public int VacationHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore di malattia (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore di malattia (in secondi).
        /// </value>
        public int SickHours { get; set; }

        /// <summary>
        /// Recupera o imposta il numero di ore di infortunio/congedo (in secondi).
        /// </summary>
        /// <value>
        /// Il numero di ore di infortunio/congedo (in secondi).
        /// </value>
        public int InjuryHours { get; set; }
        
        /// <summary>
        /// Recupera o imposta il tipo della registrazione.
        /// </summary>
        /// <value>
        /// Il tipo della registrazione.
        /// </value>
        public RegTypeEnum Type { get; set; }

        /// <summary>
        /// Recupera o imposta la data della registrazione.
        /// </summary>
        /// <value>
        /// La data della registrazione.
        /// </value>
        public DateTime DataReg { get; set; }

        /// <summary>
        /// Recupera o imposta le note della registrazione da estrarre verso Perfetto.
        /// </summary>
        /// <value>
        /// Le note della registrazione da estrarre verso Perfetto.
        /// </value>
        public string Note { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se l'istanza corrente proviene da un viaggio sotto i 10 Km.
        /// </summary>
        /// <value>
        /// <c>true</c> se l'istanza corrente proviene da un viaggio sotto i 10 Km; altrimenti, <c>false</c>.
        /// </value>
        public bool IsTripUnderKm { get; set; }

        public int DurataOre { get; set; }

        public int DurataMinuti { get; set; }

        #endregion

    }
}
