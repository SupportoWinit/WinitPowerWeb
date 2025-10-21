using Common.Properties;
using ExcelDataReader;
using Ionic.Zip;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Common
{

    [DataContract]
    public class LatLongPoint
    {
        double _latitude = 0.0d;
        double _longitude = 0.0d;

        [DataMember]
        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        [DataMember]
        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }
    }

    [DataContract]
    public class PowerAddress
    {
        private string _addressLine;
        private string _countryRegion;
        private string _district;
        private string _locality;
        private string _postalCode;
        private string _postalTown;
        private int _confidence;
        private string _displayName;
        private string _entityType;

        [DataMember]
        public string AddressLine
        {
            get { return _addressLine; }
            set { _addressLine = value; }
        }

        [DataMember]
        public string CountryRegion
        {
            get { return _countryRegion; }
            set { _countryRegion = value; }
        }

        [DataMember]
        public string District
        {
            get { return _district; }
            set { _district = value; }
        }

        [DataMember]
        public string Locality
        {
            get { return _locality; }
            set { _locality = value; }
        }
        [DataMember]
        public string PostalCode
        {
            get { return _postalCode; }
            set { _postalCode = value; }
        }

        [DataMember]
        public string PostalTown
        {
            get { return _postalTown; }
            set { _postalTown = value; }
        }
        [DataMember]
        public int Confidence
        {
            get
            {
                return _confidence;
            }
            set
            {
                _confidence = value;
            }
        }
        [DataMember]
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
        [DataMember]
        public string EntityType
        {
            get
            {
                return _entityType;
            }
            set
            {
                _entityType = value;
            }
        }
    }

    public static class CommonService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CommonService));

        public static int residualMonthMinutes = 0;


        public const string SESS_EDITFORMTEMPLATES = "EditFormTemplates";

        public static Expression<Func<T, T>> DynamicSelectGenerator<T>()
        {
            // get Properties of the T
            var fields = typeof(T).GetProperties().Select(propertyInfo => propertyInfo.Name).ToArray();

            // input parameter "o"
            var xParameter = Expression.Parameter(typeof(T), "o");

            // new statement "new Data()"
            var xNew = Expression.New(typeof(T));

            // create initializers
            var bindings = fields.Select(o => o.Trim())
                .Select(o =>
                {

                    // property "Field1"
                    var mi = typeof(T).GetProperty(o);

                    // original value "o.Field1"
                    var xOriginal = Expression.Property(xParameter, mi);

                    // set value "Field1 = o.Field1"
                    return Expression.Bind(mi, xOriginal);
                }
            );

            // initialization "new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var xInit = Expression.MemberInit(xNew, bindings);

            // expression "o => new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var lambda = Expression.Lambda<Func<T, T>>(xInit, xParameter);

            // compile to Func<Data, Data>
            return lambda;
        }

        public static List<T> ConvertTo<T>(List<object> items)
        {
            return items.ConvertAll<T>(new Converter<object, T>(delegate (object target) { return (T)target; }));
        }

        public static string GetExcelColumnNameFromInt(int index)
        {
            int dividend = index;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
        //restituisce Una LIsta unendo una Lista (target) ad un gruppo di Record (Source) letti dal DB 
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (source == null)
                throw new ArgumentNullException("source");
            foreach (var element in source)
                target.Add(element);
        }

        public static string GetErrorMessageFromDictionary(Dictionary<string, string> errors)
        //restituisce una Stringa formattata con a capo dei Record del Dizionario
        {
            StringBuilder sb = new StringBuilder();

            foreach (var error in errors)
            {
                sb.AppendFormat(" - {0}: {1}", error.Key, error.Value).AppendLine();
            }

            return sb.ToString();
        }

        public static string GetErrorMessageFromException(Exception ex, int level)
        //Formatta il testo delle Eccezioni ad albero (usta nella scrittura del LOG)
        {
            if (ex == null)
                return String.Empty;
            else
            {
                string tabString = String.Empty;

                for (int i = 0; i < level; i++)
                    tabString += "\t";

                return tabString + ex.Message + "\n" + GetErrorMessageFromException(ex.InnerException, level + 1);
            }
        }

        public static Exception GetInternalException(Exception ex)
        //restituisce la Prima Eccezione che ha generato tutto l'albero delle eccezioni
        {
            if (ex.InnerException != null)
                return GetInternalException(ex.InnerException);
            else return ex;
        }

        public static String GetPropertyName(Expression<Func<object>> exp)
        //resituisce il Nome del Campo del Record 
        {
            string name = "";

            MemberExpression body = exp.Body as MemberExpression;
            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
                name = body.Member.Name;
            }
            else
                name = body.Member.Name;
            return name;
        }

        /// <summary>
        /// Recupera il valore della proprietà specificata dall'oggetto specificato.
        /// </summary>
        /// <param name="src">L'oggetto da cui recuperare il valore della proprietà.</param>
        /// <param name="propName">Il nome della proprietà da recuperare.</param>
        /// <returns></returns>
        public static object GetPropertyValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        /// <summary>
        /// Imposta il valore specificato nella specifica proprietà nell'oggetto specifico.
        /// </summary>
        /// <param name="src">L'oggetto contenitore della proprietà da impostare.</param>
        /// <param name="propName">Il nome della proprietà da impostare.</param>
        /// <param name="value">Il valore da impostare.</param>
        public static void SetPropertyValue(object src, string propName, object value)
        {
            src.GetType().GetProperty(propName).SetValue(src, value, null);
        }

        public static byte[] ReadFully(Stream input)
        //restituisce una aRRAY IN CUI OGNI RIGA è+ UN reCORD DEL fILE rICEVUTO IN inPUT
        {
            byte[] buffer = new byte[16 * 1024];
            using (System.IO.MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static string BaseSiteUrl
        //Restituisce il Path completo dell'Applicazione (http:\....)
        {
            get
            {
                HttpContext context = HttpContext.Current;
                string baseUrl = String.Format("{0}://{1}{2}/", context.Request.Url.Scheme, context.Request.Url.Authority, context.Request.ApplicationPath.TrimEnd('/'));
                return baseUrl;
            }
        }


        #region DUPLICAZIONE ENTITA'
        /// <summary>
        ///  //Duplica un Record da una Entità ad un Altra (es CANTVAR/COLVAR e/ REG e REG_STORED)
        /// </summary>
        /// <param name="entityFrom">Enità di partenza.</param>
        /// <param name="entityTo">Entità di destinazione</param>
        public static void DuplicateEntity(Object entityFrom, Object entityTo)

        {
            //mediante reflection scorro tutte le proprietà delle entità
            foreach (PropertyInfo property in entityFrom.GetType().GetProperties())
            {
                //viene estratta la proprietà
                var prop = entityTo.GetType().GetProperty((String)property.Name);

                //viene estratto l'oggetto corrispondente alla proprietà
                Object currentValue = property.GetValue(entityFrom, null);

                if (prop != null && prop.GetSetMethod() != null)
                    prop.SetValue(entityTo, currentValue, null);
                // TODO: per il momento disabilitato perché altrimenti segnala errori anche per eventuali link mancanti
                //else
                //    throw new InvalidOperationException(String.Format("Proprietà {0} non trovata in destinazione", (String)property.Name));

            }
        }
        #endregion

        #region Strings

        /// <summary><para>La routine elabora la stringa in ingresso aggiungendo a sinistra della stringa gli spazi necessari
        /// per diventare della lunghezza indicata solo se la stringa è un numero.</para>
        /// <para>Altrimenti lascia la stringa invariata.</para></summary>
        static public string AggiungiSpaziASinistraSeStringaNumerica(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            //controllo se la stringa contiene solo numeri (tolgo eventuali spazi a sinistra prima di controllare)
            string sStringaDaValutare = sStringa.TrimStart();
            if (Regex.IsMatch(sStringaDaValutare, @"\A[0-9]+\z"))
                return CompletaASinistra(sStringaDaValutare, iLunghezzaStringa);
            else
                return sStringa;
        }

        /// <summary>La routine elabora la stringa in ingresso aggiungendo a sinistra della stringa gli spazi necessari
        /// per diventare della lunghezza indicata.</summary>
        static public string CompletaASinistra(string sStringa, int iLunghezzaStringa, char completatore = ' ')
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return sStringa.PadLeft(iLunghezzaStringa, completatore);
        }

        static public string CompletaADestra(string sStringa, int iLunghezzaStringa, char completatore = ' ')
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return sStringa.PadRight(iLunghezzaStringa, completatore);
        }

        /// <summary>
        /// Effettua il replate delle stringhe indicate solo per nella prima occorrenza.
        /// </summary>
        /// <param name="text">Il testo di cui effettuare la sostituzione.</param>
        /// <param name="search">Il testo da ricercare per la sostituzione.</param>
        /// <param name="replace">Il testo da sostituire.</param>
        /// <returns>La stringa con la sostituzione processata. </returns>
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        /// <summary>
        /// Aggiunge degli zeri a sinistra di una stringa fino a raggiungere la dimensione specificata.
        /// </summary>
        /// <param name="sStringa">La stringa da popolare con gli zeri a sinistra.</param>
        /// <param name="iLunghezzaStringa">La lunghezza da raggiungere della stringa.</param>
        /// <returns>La stringa con gli eventuali zeri a sinistra.</returns>
        public static string AggiungiZeriASinistra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaASinistra(sStringa, iLunghezzaStringa, '0');
        }
        public static string AggiungiZeriADestra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaADestra(sStringa, iLunghezzaStringa, '0');
        }

        public static string AggiungiSpaziiADestra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaADestra(sStringa, iLunghezzaStringa, ' ');
        }

        public static string AggiungiSpaziASinistra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaASinistra(sStringa, iLunghezzaStringa);
        }

        /// <summary>
        /// Costruisce una stringa a partire da un dizionario.
        /// </summary>
        /// <param name="dictionary">Il dizionario (stringa, stringa) da utilizzare per la conversione in stringa.</param>
        /// <returns>La stringa risultata dalla conversione del dizionario.</returns>
        public static String StringFromDictionary(Dictionary<String, String> dictionary)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var key in dictionary)
                sb.Append(key.Key).Append(" ").Append(dictionary[key.Key]).Append("\n");

            return sb.ToString();
        }

        /// <summary>
        /// Aggiunge il char indicato prima o dopo la stringa passata fino alla lunghezza desiderata.
        /// @param before       indica se aggiungere l'elemento in capo o in coda alla stringa
        /// @param element      l'elemento da aggiungere
        /// @param totalLength  la lunghezza che deve assumere la stringa completa
        /// @param toFill       la stringa da completare
        /// </summary>
        public static void FillWithChar(bool before, char element, int totalLength, ref String toFill)
        {
            // Recupero la lunghezza attuale della stringa
            int initialLength = toFill.Length;

            if (initialLength < totalLength)
            {
                if (before)
                {
                    // Aggiunge l'elemento in capo alla stringa fino al raggiungimento della lunghezza desiderata
                    for (int i = initialLength; i < totalLength; i++)
                    {
                        toFill = String.Concat(element, toFill);
                    }
                }

                else
                {
                    // Aggiunge l'elemento in coda alla stringa fino al raggiungimento della lunghezza desiderata
                    for (int i = initialLength; i < totalLength; i++)
                    {
                        toFill = String.Concat(toFill, element);
                    }
                }
            }
        }

        #endregion

        #region Routine di Controllo Validità IBAN/Cod.Fiscale/Partita Iva
        static public bool EIbanValido(string sCodiceIban)
        {
            //In Italia sono usati due formati:
            //- IT 02 L 12345 12345 123456789012 (12 cifre finali)
            //- IT 02 L 12345 12345 CC1234567890 (CC + 10 cifre finali)
            if (CommonService.Nz(sCodiceIban, "") == "")
                return true;
            else
                return Regex.IsMatch(sCodiceIban, @"^[a-zA-Z]{2}[0-9]{2}[a-zA-Z]{1}[0-9]{10}[a-zA-Z0-9]{12}$", RegexOptions.Multiline);
        }

        static public bool ECodiceFiscaleValido(string sCodiceFiscale)
        {
            //Formato italiano: AAAAAA11A11A111A
            //E' un controllo solo formale del formato
            if (CommonService.Nz(sCodiceFiscale, "") == "")
                return true;
            else
                return Regex.IsMatch(sCodiceFiscale, @"\A[A-Z]{6}[0-9]{2}[A-Z]{1}[0-9]{2}[A-Z]{1}[0-9]{3}[A-Z]{1}\z"); ;
        }

        static public bool EPartitaIvaValida(string sPartitaIva)
        {
            //stringa di 11 cifre
            if (CommonService.Nz(sPartitaIva, "") == "")
                return true;
            else
                return Regex.IsMatch(sPartitaIva, @"\A[0-9]{11}\z");
        }
        #endregion

        #region Routine Di Trattamento Stringhe

        static public string TogliPrincipaliAccentiAlleVocaliNellaStringa(string sStringa)
        {
            sStringa = sStringa.Replace("à", "a");
            sStringa = sStringa.Replace("è", "e");
            sStringa = sStringa.Replace("é", "e");
            sStringa = sStringa.Replace("ì", "i");
            sStringa = sStringa.Replace("ò", "o");
            sStringa = sStringa.Replace("ù", "u");
            return sStringa;
        }

        #endregion

        #region NZ Utility

        static public string Nz(string sValore, string sValoreSeNull)
        {
            return sValore ?? sValoreSeNull;
        }

        static public int? Nz(int? iValore, int? iValoreSeNull)
        {
            return iValore ?? iValoreSeNull;
        }

        static public float? Nz(float? fltValore, float? fltValoreSeNull)
        {
            return fltValore ?? fltValoreSeNull;
        }

        static public double? Nz(double? dblValore, double? dblValoreSeNull)
        {
            return dblValore ?? dblValoreSeNull;
        }

        static public byte? Nz(byte? bytValore, byte? bytValoreSeNull)
        {
            return bytValore ?? bytValoreSeNull;
        }

        static public bool? Nz(bool? bValore, bool? bValoreSeNull)
        {
            return bValore ?? bValoreSeNull;
        }

        static public DateTime? Nz(DateTime? bValore, DateTime? bValoreSeNull)
        {
            return bValore ?? bValoreSeNull;
        }

        static public TimeSpan? Nz(TimeSpan? tValore, TimeSpan? tValoreSeNull)
        {
            return tValore ?? tValoreSeNull;
        }
        #endregion

        #region DateTime Utility

        public static double GetDoubleFromMinutes(int elapsedMinutes, bool isCents = false)
        //restituisce Ore/Minuti in Sessantesimi/Centesimi dal Numero dei Minuti Ricevuti
        //es: 130 IsDecimal=treu --> 2,16    Resituisce il valore Numerico in Centesimi  
        //    130 IsDecimal =False --> 2,10  Restituisce il vaore Numerico in Sessantesimi
        {
            int hours = elapsedMinutes / 60;
            int minutes = elapsedMinutes % 60;

            if (isCents)
                minutes = Convert.ToInt32(Math.Round(Convert.ToDouble(minutes) * 100 / 60, 0));

            return (double)hours + ((double)minutes / 100d);
        }

        public static double GetDoubleFromTimeSpan(TimeSpan timespanToConvert, bool isCents = false)
        {
            int minutes = (int)timespanToConvert.TotalMinutes;

            return GetDoubleFromMinutes(minutes, isCents);
        }

        public static DateTime GetDateTimeFromMinutes(int elapsedMinutes, bool isCents = false)
        //restituisce Ore/Minuti in Sessantesimi/Centesimi dal Numero dei Minuti Ricevuti
        //es: 130 IsDecimal=true --> 2,16    Resituisce il valore Numerico in Centesimi  
        //    130 IsDecimal =False --> 2,10  Restituisce il vaore Numerico in Sessantesimi
        {
            int hours = elapsedMinutes / 60;
            int minutes = elapsedMinutes % 60;

            if (isCents)
                minutes = minutes * 100 / 60;

            return new DateTime().AddHours(hours).AddMinutes(minutes);
        }

        public static string formatDoubleToHourString(double doubleToFormat, bool isDecimalHours = false, bool doNotShowZero = false)
        {
            string hourString = "";
            string separator = isDecimalHours ? "," : ":";
            if (doubleToFormat == 0 && doNotShowZero)
            {
                hourString = "-";
            }
            else
            {
                if (doubleToFormat < 0)
                {
                    hourString = "-";
                }
                //hourString = Math.Truncate(doubleToFormat).ToString("00") + ":" + ((doubleToFormat - Math.Truncate(doubleToFormat)) * 100).ToString("00");
                hourString = string.Concat(hourString, Math.Truncate(Math.Abs(doubleToFormat)).ToString("00") + separator + ((Math.Abs(doubleToFormat) - Math.Truncate(Math.Abs(doubleToFormat))) * 100).ToString("00"));
            }

            return hourString;
            //((doubleToFormat) == 0 && doNotShowZero) ? "-" : (doubleToFormat >= 0 ? Math.Truncate(doubleToFormat).ToString("00") + ":" + ((doubleToFormat - Math.Truncate(doubleToFormat)) * 100).ToString("00") : "-" + Math.Truncate(Math.Abs(doubleToFormat)).ToString("00") + ":" + ((Math.Abs(doubleToFormat) - Math.Truncate(Math.Abs(doubleToFormat))) * 100).ToString("00"));
        }

        public static int FromHoursToMinutes(double hours, bool isDecimal = false)
        //restituisce il N° dei Minuti dalle Ore/Minuti in Sessantesimi/Centesimi  ricevute
        //es: 2,10 in Sessantesimi ---> 130 Minuti 
        //es: 2,16 in Centesimi    -->  130 Minuti
        {
            int minutes = 0;
            int min = 0;

            int intHours = (int)hours;

            if (isDecimal)
                min = Convert.ToInt32((hours - intHours) * 60d);
            else
                min = (int)((Convert.ToDecimal(hours) - Convert.ToDecimal(intHours)) * 100m);

            minutes = (intHours * 60) + min;

            return minutes;
        }

        public static DateTime GetDateTimeFromDouble(double currentValue)
        //Restituisce in Formata Data le Ore/Minuti ricevute in Centesimi in Ore/Minuti in Sessantesimi
        // 2.16 ---> 2,10
        {
            DateTime retValue = DateTime.MinValue;

            int intHour = (int)currentValue;

            int decimalHour = (int)((currentValue - intHour) * 60);

            retValue = retValue.AddHours(intHour).AddMinutes(decimalHour);

            return retValue;
        }

        public static string Get_MMM_SS_FFF_FormattedString(TimeSpan timeSpan)
        {
            return new StringBuilder().Append(timeSpan.TotalMinutes.ToString("000")).Append(":").Append(timeSpan.Seconds.ToString("00.000")).ToString();
        }

        /// <summary>
        /// Data una stringa contenente una ora HHMM viene calcolato e restituito il corrispondente timespan.
        /// </summary>
        /// <param name="hhmmString">La stringa contenente l'ora nel formato HHMM.</param>
        /// <returns>Il TimeSpan che rappresenta l'ora contenuta nella stringa passata come parametro.</returns>
        public static TimeSpan GetTimeFromHHMMString(string hhmmString)
        {
            // recupero il numero di minuti presenti nella stringa
            var minutes = Convert.ToInt32(hhmmString.Substring(2));

            // recupero il numero di ore presneti nella stringa
            var hours = Convert.ToInt32(hhmmString.Substring(0, 2));

            // ritorno il valore
            return new TimeSpan(hours, minutes, 0);
        }

        public static DateTime[] ReturnDateInterval(DateTime CurrentDate, TypoOfDateIntervalTypeEnum TypoOf)
        //Restituisce l'intervallo di Date richiesto in base al TypeOf indicato
        {
            int nDayForInit = 0;
            int nDayForEnd = 0;
            DateTime[] dateInterval = new DateTime[2];
            switch (TypoOf)
            {
                case TypoOfDateIntervalTypeEnum.Ieri:
                    dateInterval[0] = CurrentDate.AddDays(-1);
                    dateInterval[1] = CurrentDate.AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Oggi:
                    dateInterval[0] = CurrentDate;
                    dateInterval[1] = CurrentDate.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Settimana_Corrente_Fino_Al_Giorno_Incluso:
                    nDayForInit = (int)CurrentDate.DayOfWeek - 1;
                    dateInterval[0] = CurrentDate.AddDays(-nDayForInit);
                    dateInterval[1] = CurrentDate.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Settimana_Corrente_Fino_Al_Giorno_Escluso:
                    nDayForInit = (int)CurrentDate.Date.DayOfWeek - 1;
                    dateInterval[0] = CurrentDate.Date.AddDays(-nDayForInit);
                    dateInterval[1] = CurrentDate.Date.AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Settimana_Corrente_Del_Giorno:  //Tutta la Settimana che comprende il Giorno della CurrentDate ricevuta
                    nDayForInit = (int)CurrentDate.Date.DayOfWeek - 1;
                    nDayForEnd = 7 - nDayForInit;
                    dateInterval[0] = CurrentDate.Date.AddDays(-nDayForInit);
                    dateInterval[1] = CurrentDate.Date.AddDays(nDayForEnd).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Settimana_Precedente_Alla_Settimana_Del_Giorno:
                    DateTime firstWeekDay = CurrentDate.Date.StartOfWeek(DayOfWeek.Sunday);
                    dateInterval[0] = firstWeekDay.AddDays(-7);
                    dateInterval[1] = firstWeekDay;
                    break;
                case TypoOfDateIntervalTypeEnum.Mese_Corrente_del_Giorno:   //Tutto il Mese che comprende il Giorno della DataCurrent Ricevuta                 
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
                    dateInterval[1] = dateInterval[0].AddMonths(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Mese_Corrente_Fino_Al_Giorno_Incluso:
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Mese_Corrente_Fino_Al_Giorno_Escluso:
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
                    dateInterval[1] = CurrentDate.Date.AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Mese_Precedente_Al_Mese_del_Giorno:
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).AddMonths(-1);
                    dateInterval[1] = dateInterval[0].AddMonths(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Trimestre_Precedente_Al_Mese_Del_Giorno:
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).AddMonths(-3);
                    dateInterval[1] = dateInterval[0].AddMonths(3).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Semestre_Precedente_Al_Mese_Del_Giorno:
                    dateInterval[0] = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).AddMonths(-6);
                    dateInterval[1] = dateInterval[0].AddMonths(6).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Anno_Corrente_Fino_Al_Giorno_Incluso:
                    dateInterval[0] = new DateTime(CurrentDate.Year, 1, 1);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Anno_Corrente_Fino_Al_Giorno_Escluso:
                    dateInterval[0] = new DateTime(CurrentDate.Year, 1, 1);
                    dateInterval[1] = CurrentDate.Date.AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Anno_Corrente_Del_Giorno:
                    dateInterval[0] = new DateTime(CurrentDate.Year, 1, 1);
                    dateInterval[1] = dateInterval[0].AddYears(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Anno_Precedente_All_Anno_Del_Giorno:
                    dateInterval[0] = new DateTime(CurrentDate.Year, 1, 1).AddYears(-1);
                    dateInterval[1] = dateInterval[0].AddYears(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Una_Settimana_Dal_Giorno_Incluso:
                    dateInterval[0] = CurrentDate.Date.AddDays(-6);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Un_Mese_Dal_Giorno_Incluso:
                    dateInterval[0] = CurrentDate.Date.AddMonths(-1);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Tre_Mesi_Dal_Giorno_Incluso:
                    dateInterval[0] = CurrentDate.Date.AddMonths(-3);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Sei_Mesi_Dal_Giorno_Incluso:
                    dateInterval[0] = CurrentDate.Date.AddMonths(-6);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TypoOfDateIntervalTypeEnum.Un_Anno_Dal_Giorno_Incluso:
                    dateInterval[0] = CurrentDate.Date.AddYears(-1);
                    dateInterval[1] = CurrentDate.Date.AddDays(1).AddSeconds(-1);
                    break;
            }
            return dateInterval;
        }
        /// <summary>
        /// Return DateTime by a string date ex. 2014-05-21 23:59:59
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeByString(string dateString)
        {
            var toDateString = dateString.Split('-');
            var year = Int32.Parse(toDateString[0]);
            var month = Int32.Parse(toDateString[1]);

            var daySplitted = toDateString[2].Split(' ');

            var day = Int32.Parse(daySplitted[0]);
            var hour = 0;
            var minute = 0;
            var second = 0;

            if (daySplitted.Count() > 1)
            {
                var hourSplitted = daySplitted[1].Split(':');
                hour = Int32.Parse(hourSplitted[0]);
                minute = Int32.Parse(hourSplitted[1]);
                second = Int32.Parse(hourSplitted[2]);
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        /// <summary>
        /// Data una data di partenza e una data di arrivo questo metodo calcola e restituisce l'elenco
        /// di date in esso compreso. In caso di periodo non corretto o problemi nell'elaborazione viene
        /// restituita una lista vuota. 
        /// </summary>
        /// <param name="startDate">La data di partenza del periodo (compresa nella lista di ritorno)</param>
        /// <param name="endDate">La data di arrivo del periodo (compresa nella lista di ritorno)</param>
        /// <returns>Ritorna l'elenco di date compreso nel periodo passato come parametro (estremi compresi); in caso
        /// di problemi nell'elaborazione o di periodo non corretto viene restituita una lista vuota.</returns>
        public static List<DateTime> GetDatesFromPeriod(DateTime startDate, DateTime endDate)
        {
            // inizializzazione del valore di ritorno del metodo
            var returnDates = new List<DateTime>();

            // se il periodo passato come parametro è corretto
            if (startDate <= endDate)
            {
                // generazione dell'elenco di date a partire dal periodo
                returnDates = Enumerable.Range(0, 1 + endDate.Date.Subtract(startDate).Days).Select(offset => startDate.AddDays(offset)).ToList();
            }

            // ritorno del valore calcolato
            return returnDates;
        }

        /// <summary>
        /// Ritorna la striga in formato hh:mm estrando il dato dal numero di minuti passati come parametro.
        /// </summary>
        /// <param name="minutes">Il numero di minuti da processare.</param>
        /// <returns>La strinta in formato hh:mm risultato del processo del numero di minuti.</returns>
        public static string GetHHMMStringFormMinutes(int minutes)
        {
            // Se il numero è negativo, aggiungo il segno meno davanti
            String sign = "";
            if (minutes < 0)
            {
                sign = "-";
            }

            // calcolo del numero di ore in valore assoluto
            int resultHours = Math.Abs(minutes / 60);

            // calcolo del numero di minuti in valore assoluto
            int resultMinutes = Math.Abs(minutes % 60);

            // ritorno del valore calcolato
            return String.Format("{0}{1}:{2}", sign, resultHours.ToString("00"), resultMinutes.ToString("00"));
        }

        /// <summary>
        /// Questo metodo controlla che il numero passato come parametro possa rappresentare un'ora espressa in sessantesimi. 
        /// </summary>
        /// <param name="hoursToCheck">L'ora da verificare</param>
        /// <returns><c>true</c> in caso il parametro sia compatibile con l'ora in sessantesimi; altrimenti <c>false</c></returns>
        public static bool IsDoubleCompatibleWith60S(double hoursToCheck)
        {
            // inizializzazione del valore di ritorno del metodo (di default false)
            bool isCompatible = false;

            // verifico se sul numero da processare è presente una componente decimale (la cultura nella conversione a stringa
            // è fondamentale per forare il punto come separatore decimale)
            bool containsDecimal = hoursToCheck.ToString(CultureInfo.InvariantCulture).Split('.').Length == 2;

            // recupero la componente intera del numero
            var integerPart = (int)hoursToCheck;

            // recupero la parte decimale del numero (calcolata diversamente in base alla presenza o meno di decimali
            var decimalPart = containsDecimal ? Convert.ToInt32(hoursToCheck.ToString(CultureInfo.InvariantCulture).Split('.').Last()) : 0;

            // affinchè il valore compatibile la parte intera deve essere inferiore o uguake a 23
            // e la parte decimale inferiore o uguake a 59
            if (integerPart <= 23 && decimalPart <= 59)
                isCompatible = true;

            // ritorno del valore del metodo
            return isCompatible;
        }

        /// <summary>
        /// Metodo che dati il numero di minuti da processare si occupa di restituire il corrispondente TimeSpan.
        /// </summary>
        /// <param name="minutes">Il numero di minuti da processare.</param>
        /// <returns>Il TimeSpan che rappresenta il formato in minuti passato come parametro.</returns>
        public static TimeSpan GetTimeSpanFromMinutes(int minutes)
        {
            // il metodo ritorna il valore che somma alla mezzanotte il numero di minuti passati come parametro
            TimeSpan miaoa = new TimeSpan(0, minutes, 0);
            return miaoa;
        }

        /// <summary>
        /// Metodo che dato il time span rappresentante il numero di minuti da processare ne ritorna il valore intero.
        /// </summary>
        /// <param name="minutesTimeSpan">Il timespan contenente il numero di minuti da estrarre.</param>
        /// <returns>Il numero di minuti di cui è composto il timespan</returns>
        public static int GetMinutesFromTimeSpan(TimeSpan minutesTimeSpan)
        {
            // se il timespan passato come parametro è null,
            // allora si genera un'eccezione
            if (minutesTimeSpan == null)
                throw new ArgumentNullException("minutesTimeSpan");

            // il metodo ritorna il numero di minuti presenti all'interno del timespan
            // passato come parametro
            return Convert.ToInt32(minutesTimeSpan.TotalMinutes);
        }

        /// <summary>
        /// Restituisce l'inizio del mese della data passata come parametro.
        /// </summary>
        /// <param name="dateToProcess">La data da processare.</param>
        /// <returns>L'inizio del mese della data passata come parametro.</returns>
        public static DateTime GetFirstMonthDay(DateTime dateToProcess)
        {
            return new DateTime(dateToProcess.Year, dateToProcess.Month, 1);
        }

        /// <summary>
        /// Ritorna il giorno di "ieri" in base alla data passata come parametro
        /// </summary>
        /// <param name="dateToProcess">The date to process.</param>
        /// <returns></returns>
        public static DateTime Yestarday(DateTime dateToProcess)
        {
            DateTime yestarday = dateToProcess.AddDays(-1);
            return new DateTime(yestarday.Year, yestarday.Month, yestarday.Day);
        }

        /// <summary>
        /// Restituisce la fine del mese della data passata come parametro.
        /// </summary>
        /// <param name="dateToProcess">La data da processare.</param>
        /// <returns>La fine del mese della data passata come parametro.</returns>
        public static DateTime GetLastMonthDay(DateTime dateToProcess)
        {
            return new DateTime(dateToProcess.Year, dateToProcess.Month, 1).AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// Restituisce il nome breve del giorno della data passata.
        /// </summary>
        /// <param name="dateToProcess">La data da processare.</param>
        /// <returns>Il nome breve del giorno della data passata.</returns>
        public static string GetDayShortName(DateTime dateToProcess)
        {
            string dayName = dateToProcess.ToString("ddd", CultureInfo.CurrentCulture);
            return string.Format("{0}{1}", char.ToUpper(dayName[0]), dayName.Substring(1));
        }


        /// <summary>
        /// Restituisce il nome intero del giorno della data passata.   
        /// </summary>
        /// <param name="dateToProcess">La data da processare.</param>
        /// <returns>Il nome intero del giorno della data passata.</returns>
        public static string GetDayLongName(DateTime dateToProcess)
        {
            string dayName = dateToProcess.ToString("ddddd", CultureInfo.CurrentCulture);
            return string.Format("{0}{1}", char.ToUpper(dayName[0]), dayName.Substring(1));
        }

        /// <summary>
        /// Restituisce la stringa che rappresenta il mese a partire da una data.
        /// </summary>
        /// <param name="dateToProcess">La data da cui estrarre il nome del mese.</param>
        /// <returns>Il nome del mese calcolato a partire dalla data passata come parametro.</returns>
        public static string GetMonthName(DateTime dateToProcess)
        {
            return dateToProcess.ToString("MMMM", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Restituisce la stringa che rappresenta il nome corto del mese a partire da una data.
        /// </summary>
        /// <param name="dateToProcess">La data da cui estrarre il nome corto del mese.</param>
        /// <returns>Il nome corto del mese calcolato a partire dalla data passata come parametro.</returns>
        public static string GetMonthShortName(DateTime dateToProcess)
        {
            string monthName = dateToProcess.ToString("MMM", CultureInfo.CurrentCulture);
            return string.Format("{0}{1}", char.ToUpper(monthName[0]), monthName.Substring(1));
        }

        /// <summary>
        /// Fornita una data e un'ora ne combina gli elementi in un nuovo date time.
        /// </summary>
        /// <param name="dayDate">La data da processare.</param>
        /// <param name="regTime">L'ora da processare.</param>
        /// <returns>La data e ora composta dalle parti passate come parametro.</returns>
        public static DateTime ComputeDateTime(DateTime dayDate, DateTime regTime)
        {
            return new DateTime(dayDate.Year, dayDate.Month, dayDate.Day, regTime.Hour, regTime.Minute, regTime.Second);
        }

        /// <summary>
        /// Metodo che permette di recuperare (per un ciclo con foreach, utilizzo yield return) tutti i giorni di un periodo indicato.
        /// </summary>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <returns>Ritorna l'elenco dei giorni tra la data di inzio e fine periodo.</returns>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        public static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
        {
            // convalida input del meotodo, la data di arrivo deve essere maggiore della data di partenza
            if (to < from)
                throw new ArgumentException("To date must be major than from date");

            // ciclo su tutti i mesi presenti tra le due date e ritorno (utilizzando lo yield return)
            // il primo giorno di ogni mese
            for (DateTime dateIndex = from.Date; dateIndex.Date <= to.Date; dateIndex = dateIndex.AddDays(1))
                yield return dateIndex;
        }

        /// <summary>
        /// Metodo che permette di recuperare (per un ciclo con foreach, utilizzo yield return) il primo giorno di ogni mese/anno
        /// all'interno di un determinato periodo.
        /// </summary>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <returns>
        /// Ritorna l'elenco del primo giorno del mese per ogni mese compreso nel periodo dalla data di partenza alla data di arrivo.
        /// </returns>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        public static IEnumerable<DateTime> EachMonth(DateTime from, DateTime to)
        {
            // convalida input del meotodo, la data di arrivo deve essere maggiore della data di partenza
            if (to < from)
                throw new ArgumentException("To date must be major than from date");

            // porto la data di partenza e di arrivo al primo giorno del rispettivo mese
            var firstMonthDate = new DateTime(from.Year, from.Month, 1);
            var lastMonthDate = new DateTime(to.Year, to.Month, 1);

            // ciclo su tutti i mesi presenti tra le due date e ritorno (utilizzando lo yield return)
            // il primo giorno di ogni mese
            for (DateTime dateIndex = firstMonthDate.Date; dateIndex.Date <= lastMonthDate.Date; dateIndex = dateIndex.AddMonths(1))
                yield return dateIndex;
        }

        /// <summary>
        /// Metodo che permette di recuperare (per un ciclo con foreach, utilizzo yield return) il primo giorno di ogni anno
        /// all'interno di un determinato periodo.
        /// </summary>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <returns>
        /// Ritorna l'elenco del primo giorno dell'anno per ogni anno compreso nel periodo dalla data di partenza alla data di arrivo.
        /// </returns>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        public static IEnumerable<DateTime> EachYear(DateTime from, DateTime to)
        {
            // convalida input del meotodo, la data di arrivo deve essere maggiore della data di partenza
            if (to < from)
                throw new ArgumentException("To date must be major than from date");

            // porto la data di partenza e di arrivo al primo giorno del rispettivo anno
            var firstYearDate = new DateTime(from.Year, 1, 1);
            var LastYearDate = new DateTime(to.Year, 1, 1);

            // ciclo su tutti gli anni presenti tra le due date e ritorno (utilizzando lo yield return)
            // il primo giorno di ogni anno
            for (DateTime dateIndex = firstYearDate.Date; dateIndex.Date <= LastYearDate.Date; dateIndex = dateIndex.AddYears(1))
                yield return dateIndex;
        }

        /// <summary>
        /// Metodo estensione che ritorna il primo inizio settimana (identificato come parametro) antecendente a una data.
        /// </summary>
        /// <param name="dt">La data di cui cercare l'inizio della settimana.</param>
        /// <param name="startOfWeek">Il giorno di partenza della settimana.</param>
        /// <returns>La data che risulta essere il 1° giorno della settimana della data da processare </returns>
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
                diff += 7;

            return dt.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Metodo estensione che ritorna la data che rappresenta il giorno della settimana specificato successivo a quello in processo.
        /// </summary>
        /// <param name="dt">La data da processare.</param>
        /// <param name="dayOfWeek">Il giorno della settimana da ricercare.</param>
        /// <returns>
        /// La data che rappresenta il giorno specificato successivo a quello in processo.
        /// </returns>
        public static DateTime NextDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek)
        {
            DateTime returnValue = dt;

            for (int i = 1; i <= 7; i++)
            {
                returnValue = returnValue.AddDays(1);
                if (returnValue.DayOfWeek == dayOfWeek)
                    break;
            }

            return returnValue;
        }

        /// <summary>
        /// Metodo estensione che ritorna la data che rappresenta il giorno della settimana specificato precedente a quello in processo.
        /// </summary>
        /// <param name="dt">La data da processare.</param>
        /// <param name="dayOfWeek">Il giorno della settimana da ricercare.</param>
        /// <returns>
        /// La data che rappresenta il giorno specificato precedente a quello in processo.
        /// </returns>
        public static DateTime PrevDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek)
        {
            DateTime returnValue = dt;

            for (int i = 1; i <= 7; i++)
            {
                returnValue = returnValue.AddDays(-1);
                if (returnValue.DayOfWeek == dayOfWeek)
                    break;
            }

            return returnValue;
        }

        /// <summary>
        /// Recupera la differenza in giorni tra le due date specificate.
        /// </summary>
        /// <param name="startDate">La data estremo inferiore con cui calcolare la differenza.</param>
        /// <param name="endDate">La data estremo superiore con cui calcolare la differenza.</param>
        /// <returns>Il numero di giorni di differenza tra l'estremo inferiore e superiore specificati.</returns>
        public static int GetDayDifference(DateTime startDate, DateTime endDate)
        {
            return (endDate - startDate).Days;
        }

        /// <summary>
        /// Determina se le due date/ore specificate risultano nello stesso minuto.
        /// </summary>
        /// <param name="firstDateTime">La prima data su cui effettuare il confronto.</param>
        /// <param name="secondDateTime">La seconda data su cui effettuare il confronto.</param>
        /// <returns><c>true</c> se le due date risultano essere nello stesso minuto; altrimenti <c>false</c></returns>
        public static bool IsOnSameMinute(DateTime firstDateTime, DateTime secondDateTime)
        {
            // le due date risultano essere nello stesso minuto se condividono tutti i dati fino alla precisione del minuto
            return firstDateTime.Year == secondDateTime.Year && firstDateTime.Month == secondDateTime.Month
                   && firstDateTime.Day == secondDateTime.Day && firstDateTime.Hour == secondDateTime.Hour && firstDateTime.Minute == secondDateTime.Minute;
        }

        /// <summary>
        /// Recupera l'ultimo giorno specificato del mese rappresentato dalla data indicata.
        /// </summary>
        /// <param name="dt">La data di riferimento per il calcolo.</param>
        /// <param name="dayToSearch">Il giorno da ricercare.</param>
        /// <returns>L'ultimo giorno specificato del mese rappresentato dalla data indicata.</returns>
        public static DateTime GetLastDayOfWeekInMonth(DateTime dt, DayOfWeek dayToSearch)
        {
            // inizializzazione del valore di ritorno del metodo
            DateTime returnValue = DateTime.MinValue;

            // si recupera l'ultimo giorno del mese
            DateTime lastMonthDay = GetLastMonthDay(dt);

            // si cicla a ritroso fino a incontrare l'ultimo giorno indicato del mese
            for (int i = lastMonthDay.Day; i > 0; i--)
            {
                // recupero del giorno in processo
                var checkDay = new DateTime(lastMonthDay.Year, lastMonthDay.Month, i);

                // se si sta elaborando il giorno richiesto allora si procede a
                // impostarlo come ritorno del metodo e si interrompe il ciclo attualmente in corso 
                if (checkDay.DayOfWeek == dayToSearch)
                {
                    returnValue = checkDay;
                    break;
                }
            }

            // ritorno del giorno calcolato dal metodo
            return returnValue;
        }

        /// <summary>
        /// Recupera il primo giorno specificato nel mese rappresentato dalla data indicata.
        /// </summary>
        /// <param name="dt">La data di riferimento per il calcolo.</param>
        /// <param name="dayToSearch">Il giorno da ricercare.</param>
        /// <returns>Il primo giorno specificato nel mese rappresentato dalla data indicata.</returns>
        public static DateTime GetFirstDayOfWeekInMonth(DateTime dt, DayOfWeek dayToSearch)
        {
            // inizializzazione del valore di ritorno del metodo
            DateTime returnValue = DateTime.MinValue;

            // si recupera l'ultimo giorno del mese
            DateTime lastMonthDay = GetLastMonthDay(dt);

            // si cicla da inizio a fine mese fino a trovare il primo giorno indicato del mese
            for (int i = 1; i <= lastMonthDay.Day; i++)
            {
                // recupero del giorno in processo
                var checkDay = new DateTime(lastMonthDay.Year, lastMonthDay.Month, i);

                // se si sta elaborando il giorno richiesto allora si procede a
                // impostarlo come ritorno del metodo e si interrompe il ciclo attualmente in corso 
                if (checkDay.DayOfWeek == dayToSearch)
                {
                    returnValue = checkDay;
                    break;
                }
            }

            // ritorno del giorno calcolato dal metodo
            return returnValue;
        }

        /// <summary>
        /// Recupera il numero di minuti dalla stringa formattata come HH:MM.
        /// La stringa specificata può anche contenere all'inizio un carattere per indicare il segno negativo del numero di minuti da tornare (-)
        /// </summary>
        /// <param name="hhmmString">
        /// La stringa da processare nel format HH:MM.
        /// La stringa specificata può anche contenere all'inizio un carattere per indicare il segno negativo del numero di minuti da tornare (-)
        /// </param>
        /// <returns>Il numero di minuti dalla stringa formattata come HH:MM; ritorna 0 in caso di errore di calcolo o di stringa vuota o nulla.</returns>
        public static int GetMinutesNumberFromHHMMString(string hhmmString)
        {
            int returnValue = 0;

            try
            {
                // se la stringa presenta un valore
                if (!string.IsNullOrEmpty(hhmmString))
                {
                    // calcolo del primo indice a seconda della presenza del primo carattere di segno
                    int startIndex = hhmmString.First() == '-' ? 1 : 0,
                        dividerIndex = hhmmString.IndexOf('.') & hhmmString.IndexOf(':'),
                        hourLength = dividerIndex >= 0 ? dividerIndex - startIndex : hhmmString.Length - startIndex;

                    string hoursString = string.IsNullOrEmpty(hhmmString.Substring(startIndex, hourLength)) ? "0" : hhmmString.Substring(startIndex, hourLength);
                    string minutesString;
                    try
                    {
                        minutesString = hhmmString.Substring(dividerIndex + 1, hhmmString.Length - hourLength - 1 - startIndex);
                    }
                    catch
                    {
                        minutesString = "0";
                    }

                    // la componente ora viene aggiunta moltiplicata per 60 al numero di minuti
                    returnValue += Convert.ToInt32(hoursString) * 60;

                    // la componente minuti invece viene aggiunta semplice
                    returnValue += Convert.ToInt32(minutesString);

                    // se è stato passato un segno negativo alla procedura allora si procede a moltiplicare il valore
                    // per renderlo negativo
                    if (startIndex == 1)
                        returnValue *= -1;
                }
            }
            catch (Exception ex)
            {

            }

            return returnValue;
        }

        #endregion

        #region Number Utilities

        /// <summary>
        /// Genera e restituisce un numero intero generato ramdom.
        /// </summary>
        /// <returns>L'intero costruito generando random le cifre che lo compongono</returns>
        public static int GetRandomNumber()
        {
            var bytes = new byte[4];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0) % 100000000;
        }

        /// <summary>
        /// Somma due numeri di tipo double trattandoli come ore,minuti.
        /// </summary>
        /// <returns>I due numeri sommati</returns>
        public static double SumDoubleHours(double first, double second, bool isDecimalHours = false)
        {
            double result;
            double hours = Math.Truncate(first) + Math.Truncate(second);
            double minutes = Math.Round((first - Math.Truncate(first)) * 100) + Math.Round((second - Math.Truncate(second)) * 100);

            if (!isDecimalHours)
            {
                hours += Math.Floor(minutes / 60);
                minutes = minutes % 60;
            }

            result = hours + (minutes / 100);

            return result;
        }

        /// <summary>
        /// Sottrae due numeri di tipo double trattandoli come ore,minuti.
        /// ATTENZIONE!! NON GESTISCE I CASI QUANDO LA DIFFERENZA < ZERO
        /// </summary>
        /// <returns>I due numeri sottratti</returns>
        public static double SubtractDoubleHours(double first, double second, bool isDecimalHours = false)
        {
            double result,
                   minutes,
                   hours,
                   minutes1 = Math.Round((first - Math.Truncate(first)) * 100),
                   minutes2 = Math.Round((second - Math.Truncate(second)) * 100),
                   hours1 = Math.Truncate(first),
                   hours2 = Math.Truncate(second),
                   unitBase = isDecimalHours ? 100 : 60;

            if (minutes2 > minutes1)
            {
                minutes = unitBase - (minutes2 - minutes1);
                hours = hours1 - hours2 - 1;
            }
            else
            {
                minutes = minutes1 - minutes2;
                hours = hours1 - hours2;
            }

            result = hours + minutes / 100;

            return result;
        }

        /// <summary>
        /// Determina se il valore double passato come parametro può essere considerato (applicativamente) a zero (secondo la precisone di 4 cifre decimali).
        /// </summary>
        /// <param name="doubleToCheck">Il double di cui verificare la prossimità a zero.</param>
        /// <returns><c>true</c> in cui il double possa essere trattato come zero; altrimenti <c>false</c></returns>
        public static bool IsDoubleZero(double doubleToCheck)
        {
            return (Math.Abs(doubleToCheck - 0.00) <= 0.00001);
        }

        #endregion

        #region Excel utilities

        /// <summary>
        /// Converte il file excel al percorso di origine nel csv nel percorso di destinazione.
        /// </summary>
        /// <param name="excelFileName">Il nome del file excel originale.</param>
        /// <param name="destinationFile">Il nome del file csv di destinazione.</param>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.InvalidOperationException">Formato file non valido!</exception>
        public static void ConvertExcelFileIntoCsv(string excelFileName, string destinationFile)
        {
            if (!File.Exists(excelFileName))
                throw new FileNotFoundException(excelFileName);

            using (var tmpFileReader = File.Open(excelFileName, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader dataReader = null;

                // procedo alla conversione del file passato come parametro (viene letto diversamente il base al formato excel)
                switch (Path.GetExtension(excelFileName).ToUpper())
                {
                    case ".XLSX":
                        dataReader = ExcelReaderFactory.CreateOpenXmlReader(tmpFileReader);
                        break;
                    case ".XLS":
                        dataReader = ExcelReaderFactory.CreateBinaryReader(tmpFileReader);
                        break;
                    default:
                        throw new InvalidOperationException("Formato file non valido!");
                }

                // lettura dei dati del file excel
                var result = dataReader.AsDataSet();

                // chiusura dello stream e rilascio delle risorse
                dataReader.Close();

                // preparazione dei dati e scrittura del file csv
                string csvData = "";
                int row_no = 0;

                // si legge sempre e solo il primo worksheet
                int ind = 0;

                while (row_no < result.Tables[ind].Rows.Count)
                {
                    for (int i = 0; i < result.Tables[ind].Columns.Count; i++)
                    {
                        csvData += result.Tables[ind].Rows[row_no][i].ToString() + ";";
                    }
                    row_no++;
                    csvData += "\r";
                }

                csvData = String.Join("\r", csvData.Split('\r').Where(line => !String.IsNullOrEmpty(line)));

                using (var csv = new System.IO.StreamWriter(destinationFile, false, Encoding.UTF8))
                {
                    csv.Write(csvData);
                    csv.Close();
                }
            }
        }

        /// <summary>
        /// Torna la(e) lettera(e) corrispondente al numero di colonna.
        /// </summary>
        /// <param name="index">L'indice della colonna.</param>
        /// <returns></returns>
        public static string GetColumnName(int index)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var value = "";

            if (index >= letters.Length)
                value += letters[index / letters.Length - 1];

            value += letters[index % letters.Length];

            return value;
        }

        #endregion

        #region Files Management

        /// <summary>
        /// Compatta in un file zip l'elenco di files richiesti.
        /// </summary>
        /// <param name="filesToZip">L'elenco dei files da compattare nello zip.</param>
        /// <param name="zipFilePath">Il file zip di destinazione della compattazione dei files.</param>
        /// <exception cref="System.ArgumentNullException">filesToZip
        /// or
        /// zipFilePath</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">filesToZip</exception>
        /// <exception cref="System.IO.FileNotFoundException">File to zip not found</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">zip destination directory not found</exception>
        /// <exception cref="System.IO.IOException">zip file yet present</exception>
        public static void ZipFilesList(IEnumerable<string> filesToZip, string zipFilePath)
        {

            #region Convalida input del metodo

            // convalida input del metodo - files da zippare
            if (filesToZip == null)
                throw new ArgumentNullException("filesToZip");
            if (!filesToZip.Any())
                throw new ArgumentOutOfRangeException("filesToZip");
            foreach (string fileToZip in filesToZip)
                if (!System.IO.File.Exists(fileToZip))
                    throw new FileNotFoundException("File to zip not found", fileToZip);

            // convalida input del metodo - percorso file dello zip
            if (String.IsNullOrEmpty(zipFilePath))
                throw new ArgumentNullException("zipFilePath");
            if (!Directory.Exists(Path.GetDirectoryName(zipFilePath)))
                throw new DirectoryNotFoundException("zip destination directory not found");
            if (File.Exists(zipFilePath))
                throw new IOException("zip file yet present");

            #endregion

            // generazione del nuovo archivio zip, importazione dei files e salvataggio
            // degli stessi nella destinazione
            using (ZipFile newZipArchive = new ZipFile())
            {
                newZipArchive.Encryption = EncryptionAlgorithm.None;
                foreach (string fileToZip in filesToZip)
                    newZipArchive.AddFile(fileToZip, @"\");

                newZipArchive.Save(zipFilePath);
            }

        }

        #endregion

        #region Gestione codici

        /// <summary>
        /// Determina se lo specifico codice attività risulta essere un'attività specificata su dispositivo.
        /// </summary>
        /// <param name="codeToCheck">Il codice da verificare.</param>
        /// <returns><c>true</c> in caso il codice specificato risulti essere un'attività di dispositivo; altrimenti <c>false</c></returns>
        public static bool IsActivityDeviceCode(string codeToCheck)
        {
            // NB: la posizione naturale di questo metodo sarebbe nel BusinessService, messa qua in quanto utilizzata da proprietà
            //     nel progetto Domain.
            // Il codice passato come parametro, se non vuoto, risulta essere un codice attività arrivato da dispositivo
            // se, una volta tolti gli spazi superflui, risulta cominciare per uno dei codici configurati nei settings

            // inizializzazione del valore di ritorno del metodo
            bool returnValue = false;

            // se il codice passato come parametro risulta valorizzato
            if (!String.IsNullOrEmpty(codeToCheck))
                returnValue = Settings.Default.DeviceActivityBaseCodes.Any(baseCode => codeToCheck.Trim().StartsWith(baseCode));

            // ritorno del valore calcolato dal metodo
            return returnValue;
        }

        #endregion

        #region Invio email        
        /// <summary>
        /// Invia mail.
        /// </summary>
        /// <param name="toAddress">Indirizzi a cui inviare la mail (da separare con ';').</param>
        /// <param name="mailSubject">Oggetto della mail.</param>
        /// <param name="mailBody">Testo (in HTML) della mail.</param>
        /// <param name="fromAddress">L'indirizzo da cui si vuole inviare la mail (non deve per forza essere un indirizzo esistente).</param>
        /// <param name="fromAddressName">Nome da dare all'indirizzo del mittente (generalmente visualizzato dai client di posta).</param>
        /// <param name="attachments">Percorso di eventuali allegati.</param>
        /// <returns></returns>
        public static string sendMail(string toAddress, string mailSubject, string mailBody, string fromAddress, string fromAddressName, string[] attachments)
        {
            try
            {
                //Crea l'oggetto indirizzo mail dal quale inviare la mail
                MailAddress fromAddressObject = new MailAddress(fromAddress, fromAddressName);

                // Salva in un array gli indirizzi dei destinatari divisi da ';')
                string[] mailAddresses = toAddress.Split(';');
                MailAddress[] toAddresses = new MailAddress[mailAddresses.Length];

                // Converto le stringhe in MailAddresses
                for (int i = 0; i < mailAddresses.Length; i++)
                {
                    toAddresses[i] = new MailAddress(mailAddresses[i]);
                }

                //Password dell'account dell'indirizzo del mittente
                const string fromPassword = "Power2025!";

                //Prepara il client SMTP
                var smtp = new SmtpClient
                {
                    Host = "mx9.zimbra-ilger.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("servizi@winitsrl.it", fromPassword)
                };

                //Imposta il timout al massimo, per evitare che blocchi l'operazione quando ci sono molti allegati
                smtp.Timeout = 600000;
                //Prepara l'email per l'invio
                var mail = new MailMessage();
                mail.From = fromAddressObject;
                //Aggiunge tutti gli indirizzi di destinazione
                foreach (MailAddress email in toAddresses)
                {
                    mail.To.Add(email);
                }
                //Imposta oggetto e testo della mail
                mail.IsBodyHtml = true;
                mail.Body = mailBody;
                mail.Subject = mailSubject;

                //Allega gli allegati richiesti
                foreach (string attachmentPath in attachments)
                {
                    //Controlla che esista il file specificato prima di allegarlo
                    if (File.Exists(attachmentPath))
                    {
                        Attachment attachment = new Attachment(attachmentPath);
                        mail.Attachments.Add(attachment);
                    }
                }

                //Invia l'email
                smtp.Send(mail);
            }

            catch (Exception ex)
            {
                _log.Error(String.Format("{0} - {1}", "Si è verificato un errore nell'invio della mail: ", ex.ToString()));
                return "C'è stato un errore nell'invio della mail, riprovare più tardi o contattare l'assistenza";
            }

            return "Mail inviata!";
        }
        #endregion


    }

}