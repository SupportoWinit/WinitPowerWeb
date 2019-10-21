using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Westwind.Utilities;

namespace Domain
{
    public partial class Reg_V
    {

        public bool Associated                      //Restituisce True/False in Campo Associata se Durata_Fis <> 0 oppure =0
        {
            get { return Durata_Fis.HasValue; }
        }
        //public DateTime Data_Ora_Fig_ETime          //Restituisce solo la parte dell'Ora del campo Data/Ora ENTRATA FIGURATIVA
        //{
        //    get
        //    {

        //        var currentValue = DateTime.MinValue;
        //        if (Data_Ora_Fig_E.HasValue)
        //            currentValue = new DateTime().Add(Data_Ora_Fig_E.Value.TimeOfDay);
        //        return currentValue;

        //    }
        //}
        //public DateTime Data_Ora_Fis_ETime          //Restituisce solo la parte dell'Ora del campo Data/Ora ENTRATA FISICA
        //{
        //    get { return new DateTime().Add(Data_Ora_Fis_E.TimeOfDay); }
        //}
        //public DateTime Data_Ora_Fig_UTime          //Restituisce solo la parte dell'Ora del campo Data/Ora USCITA FIGURATIVA
        //{
        //    get
        //    {
        //        var currentValue = DateTime.MinValue;
        //        if (Data_Ora_Fig_U.HasValue)
        //            currentValue = new DateTime().Add(Data_Ora_Fig_U.Value.TimeOfDay);
        //        return currentValue;
        //    }
        //}
        //public DateTime Data_Ora_Fis_UTime          //Restituisce solo la parte dell'Ora del campo Data/Ora USCITA FISICA
        //{
        //    get
        //    {
        //        var currentValue = DateTime.MinValue;
        //        if (Data_Ora_Fis_U.HasValue)
        //            currentValue = new DateTime().Add(Data_Ora_Fis_U.Value.TimeOfDay);
        //        return currentValue;
        //    }
        //}
        //public DateTime Data_Ora_Fig_EDate          //restituisce solo la parte della Data del campo Data/Ora ENTRATA FIGURATIVA 
        //{
        //    get
        //    {
        //        var currentValue = DateTime.MinValue;
        //        if (Data_Ora_Fig_E.HasValue)
        //            currentValue = Data_Ora_Fig_E.Value.Date;
        //        return currentValue;
        //    }
        //}
        //public DateTime Data_Ora_Fis_EDate          //restituisce solo la parte della Data del campo Data/Ora ENTRATA FISICA
        //{
        //    get { return Data_Ora_Fis_E.Date; }
        //}
        //public DateTime Data_Reg                //restituisce la Data Registrazione di una REG_V (la prende dal campo Data/Ora ENTRATA FISICA)
        //{
        //    get { return Data_Ora_Fis_E.Date; }
        //}
        public DateTime Data_Ora_FigFis_E           //Restituisce la Data/Ora Figurativa di Entrata se <>0 altrimenti la Data/Ora Fisica
        {
            get
            {
                if (Data_Ora_Fig_E.HasValue)
                    return Data_Ora_Fig_E.Value;
                else
                    return Data_Ora_Fis_E;
            }
        }
        public DateTime? Data_Ora_FigFis_U          //Restituisce la Data/Ora Figurativa di Uscita se <>0 altrimenti la Data/Ora Fisica
        {
            get
            {
                if (Data_Ora_Fig_U.HasValue)
                    return Data_Ora_Fig_U.Value;
                else if (Data_Ora_Fis_U.HasValue)
                    return Data_Ora_Fis_U.Value;
                else return null;
            }
        }
        public DateTime Durata_Fig_HH_C                 //Restituisce la Durata FISICA in formato DateTime
        {
            get
            {
                var currentValue = DateTime.MinValue;
                if (Durata_Fig.HasValue)
                    currentValue = CommonService.GetDateTimeFromMinutes(Durata_Fig.Value, true);
                return currentValue;
            }
        }
        public DateTime Durata_Fis_HH_C                 //Restituisce la Durata FISICA in formato DateTime
        {
            get
            {
                var currentValue = DateTime.MinValue;
                if (Durata_Fis.HasValue)
                    currentValue = CommonService.GetDateTimeFromMinutes(Durata_Fis.Value);
                return currentValue;
            }
            set
            {
                Durata_Fis = CommonService.GetMinutesFromTimeSpan(value.TimeOfDay);
            }
        }

        public string Short_Week_Day
        {
            get
            {
                string shortDay = "";
                if (Data_Reg.HasValue)
                {
                    shortDay = CommonService.GetDayShortName(Data_Reg.Value);
                }
                return shortDay;
            }
        }
        
        public string Long_Week_Day
        {
            get
            {
                string longDay = "";
                if (Data_Reg.HasValue)
                {
                    longDay = CommonService.GetDayLongName(Data_Reg.Value);
                }
                return longDay;
            }
        }


        


        /// <summary>
        /// Rappresenta un valore che segnala se l'ora in uscita, in fase di inserimento, fa pate dello stesso giorno dell'ora di entrata.
        /// Questo flag viene utilizzato solamente se è abilitato il notturno e l'ora di uscita è inferirore all'ora di entrata, e serve
        /// a invertire ora di entrata e uscita nella regV al fine di preservare la data/ora modifica originale
        /// </summary>
        private bool _isUTimeSameDayE = false;

        /// <summary>
        /// Recupera un valore che segnala se l'ora in uscita, in fase di inserimento, fa pate dello stesso giorno dell'ora di entrata.
        /// Questo flag viene utilizzato solamente se è abilitato il notturno e l'ora di uscita è inferirore all'ora di entrata, e serve
        /// a invertire ora di entrata e uscita nella regV al fine di preservare la data/ora modifica originale
        /// </summary>
        /// <value>
        /// Recupera un valore che segnala se l'ora in uscita, in fase di inserimento, fa pate dello stesso giorno dell'ora di entrata.
        /// Questo flag viene utilizzato solamente se è abilitato il notturno e l'ora di uscita è inferirore all'ora di entrata, e serve
        /// a invertire ora di entrata e uscita nella regV al fine di preservare la data/ora modifica originale
        /// </value>
        public bool IsUTimeSameDayE
        {
            get { return _isUTimeSameDayE; }
            set { _isUTimeSameDayE = value; }
        }

        /// <summary>
        /// L'identificativo univoco utilizzato nella gestione delle registrazioni errate per collegare i nuovi valori
        /// inseriti ma non ancora salvati su database
        /// </summary>
        private Guid? _tmpNewId = null;

        /// <summary>
        /// Recupera l'identificativo utilizzato nella gestione delle registrazioni errate per collegare i nuovi valori inseriti ma non acora salvati sul database.
        /// </summary>
        /// <value>
        /// L'identificativo utilizzato utilizzato nella gestione delle registrazioni errate per collegare i nuovi valori inseriti ma non ancora salvati sul database
        /// </value>
        public string TmpNewId
        {
            get
            {
                return _tmpNewId == null ? String.Empty : _tmpNewId.ToString();
            }
        }

        /// <summary>
        /// Genera un nuovo identificativo utilizzato nella gestione delle registrazioni errate per collegare i nuovi valori inseriti ma non ancora salvati sul database.
        /// </summary>
        public void GenerateTmpId()
        {
            _tmpNewId = Guid.NewGuid();
        }

        /// <summary>
        /// Recupera o imposta il campo temporaneo utilizzato per appoggiare la data/ora figurativa d'entrata.
        /// </summary>
        /// <value>
        /// Il campo temporaneo utilizzato per appoggiare la data/ora figurativa d'entrata.
        /// </value>
        public DateTime? TmpDataOraFigE { get; set; }

        /// <summary>
        /// Recupera o imposta il campo temporaneo utilizzato per appoggiare la data/ora figurativa d'uscita.
        /// </summary>
        /// <value>
        /// Il campo temporaneo utilizzato per appoggiare la data/ora figurativa d'uscita.
        /// </value>
        public DateTime? TmpDataOraFigU { get; set; }

        /// <summary>
        /// Recupera o imposta il campo temporaneo utilizzato per appoggiare la durata figurativa.
        /// </summary>
        /// <value>
        /// Il campo temporaneo utilizzato per appoggiare la durata figurativa.
        /// </value>
        public int? TmpDurataFigU { get; set; }
        
        /// <summary>
        /// Recupera o imposta il valore booleano che indica se la Reg_V corente è una registrazione di sola durata o meno;
        /// se il campo risulta null indica che è necessario verificare il tipo registrazione per comprenderlo.
        /// L'utilizzo di questo campo è necessario per capire quando è l'utente che decide se la registrazione è di sola durata.
        /// </summary>
        private bool? _isOnlyDuration;

        /// <summary>
        /// Recupera o imposta il valore booleano che indica se la Reg_V corrente è una registrazione di sola durata o meno.
        /// </summary>
        /// <value>
        /// il valore booleano che indica se la Reg_V corrente è una registrazione di sola durata o meno.
        /// </value>
        public bool IsOnlyDuration
        {
            get
            {
                // se è stato impostato un valore sull'estensione allora lo si ritorna; altrimenti si utilizza il valore nel tipo registrazione
                return _isOnlyDuration ?? Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration;
            }

            set
            {
                // impostazione del campo di sola durata
                _isOnlyDuration = value;

                // impostazione del campo tipo registrazione
                Registrazione_Tipo_Reg = value ? (int)RegTypeEnum.Duration : (int)RegTypeEnum.None;
            }
        }

        

        /// <summary>
        /// Recupera o imposta il valore che indica se la registrazione corrente è un viaggio trattato come ore lavorate (Export 17).
        /// </summary>
        /// <value>
        /// <c>true</c> indica che laregistrazione corrente è un viaggio trattato come ore lavorate (Export 17; altrimenti, <c>false</c>.
        /// </value>
        public bool WasTrip { get; set; }

        /// <summary>
        /// <c>true</c> se la registrazione ha una durata negativa; altrimenti, <c>false</c>.
        /// Questo valore è utilizzato solamente in fase di data input per il calcolo del segno di durata e non è salvato a database.
        /// Questo valore è utilizzato solamente per registrazioni di tipo solo durata.
        /// </summary>
        private bool? _registrationDurationNegative;

        /// <summary>
        /// Recupera o imposta il valore che indica se la reg_v corrente ha durata negativa o meno.
        /// Questo valore è utilizzato solamente in fase di data input per il calcolo del segno di durata e non è salvato a database.
        /// Questo valore è utilizzato solamente per registrazioni di tipo solo durata.
        /// </summary>
        /// <value>
        /// <c>true</c> se la registrazione ha una durata negativa; altrimenti, <c>false</c>.
        /// Questo valore è utilizzato solamente in fase di data input per il calcolo del segno di durata e non è salvato a database.
        /// Questo valore è utilizzato solamente per registrazioni di tipo solo durata.
        /// </value>
        public bool RegistrationDurationNegative
        {
            get { return _registrationDurationNegative ?? Durata_Fis < 0; }
            set
            {
                _registrationDurationNegative = value;

                if ((value && Durata_Fis >= 0 && IsOnlyDuration) || (!value && Durata_Fis < 0 && IsOnlyDuration))
                    Durata_Fis *= -1;
            }
        }
    }
}
