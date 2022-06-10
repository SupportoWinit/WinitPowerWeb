using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exports.ExportTxtCustom
{
    /**
     * Export specifico del cliente Vecomp in formato .txt
     * Export per periodo mensile
     * Per ogni collaboratore, genera tanti record quante sono le causali presenti per il dato giorno.
     * Ogni record avrà il seguente tracciato:
     *  - Codice ditta*             -> codice cliente, 5 char -> ('00000')
     *  - Posizione INPS*           -> FISSO '00', 2 char -> ('00')
     *  - Codice dipendete          -> codice del collaboratore, 5 char -> ('00000')
     *  - Data di rilevazione       -> data della timbratura, 6 char -> ('AAMMGG')
     *  - Codice causale            -> se minore NO riempimento ma si lascia spazio, 3 char -> ('ORD')
     *  - Ora dell'evento           -> (?), 4 char in centesimi -> ('0000')
     *  - Ore lavorate complessive  -> delta delle ore lavorate, 4 char in centesimi -> ('0000')
     *  - Giorno lavorativo*        -> enum: 0 = lavorativo (lun-ven) | 1 = non_lavorativo (sab) | 2 = festivo (dom), 1 char -> ('2')
     *  - Tipo elaborazione*        -> FISSO '0', 1 char -> ('0')
     * 
     * 
     *      *=facoltativo
     **/

    class ExportTxtVecomp : TxtToolBox
    {
        #region Private Properties

        // CONSTANTS
        private const string _tipoElaborazione = "0";

        // VARIABLES
        private string _year;
        private string _month;
        private string _monthString;
        private string _codiceDitta;
        private string _posizioneInps;
        private string _codiceDipendente = "00000";
        private string _dataRilevazione = "AAMMGG";
        private string _codiceCausale = "ORD";
        private string _oraEvento;
        private string _oreLavorateComplessive;

        // ENUMS
        private enum _giornoLavorativo { lavorativo = 0, non_lavorativo = 1, festivo = 2 };

        #endregion

        #region Constructor

        #endregion

        #region Public Methods
        public override void LaunchExport()
        {
            _year = ExportDate.ToString("yy");
            _month = ExportDate.Month.ToString("mm");

            _monthString = ExportDate.Month.ToString();

            var allowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id)).Select(c => new { c.Col_Id, c.Codice_Collaboratore }).ToList();

            FileName = "presenze_" + _monthString;
            Extension = "txt";


            foreach (var col in allowedColls)
            {
                int oreLavorate = 5; //rnd
                int oreEvento = 5; //rnd
                DateTime rilevazione = new DateTime(2021, 5, 1); // rnd 


                _codiceDitta = Add0Left5(_codiceDitta);
                _posizioneInps = Add0Left2(_posizioneInps);
                _codiceDipendente = col.Codice_Collaboratore;
                _dataRilevazione = FromDateTimeToString(rilevazione);
                _oraEvento = ToCent(oreEvento).ToString();
                _oreLavorateComplessive = ToCent(oreLavorate).ToString();

                DayOfTheWeek(rilevazione);

                TxtLines.Add(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}",
                    _codiceDitta,                   //0
                    _posizioneInps,                 //1
                    _codiceDipendente,              //2
                    _dataRilevazione,               //3
                    _codiceCausale,                 //4
                    _oraEvento,                     //5
                    _oreLavorateComplessive,        //6
                                                    //7
                    _tipoElaborazione               //8
                    ));
            }


        }

        #endregion

        #region Private Methods

        private string Add0Left5(string str)
        // Aggiunge a sinistra gli zeri mancanti per riempire il campo
        {
            int n_char = str.Count();

            switch (n_char)
            {
                case 1:
                    str = "0000" + str;
                    break;
                case 2:
                    str = "000" + str;
                    break;
                case 3:
                    str = "00" + str;
                    break;
                case 4:
                    str = "0" + str;
                    break;
                case 5:
                    break;
            }

            return str;
        }

        private string Add0Left2(string str)
        {
            int n_char = str.Count();

            switch (n_char)
            {
                case 1:
                    str = "0" + str;
                    break;
                case 2:
                    break;
            }

            return str;
        }

        private int ToCent(int minutes)
        {
            return Convert.ToInt32((minutes / 60.0) * 100);
        }

        private string FromDateTimeToString(DateTime date)
        {
            string str = date.ToString("yy" + "mm" + "gg");

            return str;
        }

        private int DayOfTheWeek(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday)
                return 1;
            else if (date.DayOfWeek == DayOfWeek.Sunday)
                return 2;
            else
                return 0;
        }
        #endregion
    }
}
