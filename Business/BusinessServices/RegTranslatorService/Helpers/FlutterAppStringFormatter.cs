using System;
using System.Collections.Generic;
using Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegTranslatorService.Helpers
{
    class FlutterAppStringFormatter
    {

            private static readonly string TIPO_ATTIVITA = "TIPOATT";
            private static readonly string SQUADRA = "SQUADRA";
            private static readonly string PRUCODE = "PRUCODEFORACTIVITY";
            private static readonly string SOTTOCANTIERE = "SUBCANT";
            private static readonly string TURNO = "TURNO";
            private static readonly string INFOAGG = "INFOAGG";
            private static readonly string NOTE = "NOTE";

            private static readonly int MARKER_LATITUDINE = 0;
            private static readonly int MARKER_LONGITUDINE = 1;
            private static readonly int TAG_REFERENCE = 1;
            private static readonly int NO_TAG_REFERENCE = 0;

            private static readonly string SEPARATOR = ";";

            #region STRING FORMATS

            private static readonly string NFC_QRCODE_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}";
            private static readonly string GPS_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{0}";
            private static readonly string EXTRA_INFO_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}";
            private static readonly string FIRST_INFO_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}";
            #endregion

        internal static string CreateFirstActivityLines(string deviceCode, string badgeCode, DateTime regDateTime, string activityTypeCode)
            {
                if (String.IsNullOrEmpty(activityTypeCode))
                    return "";

                string activityLines = "";

                foreach (string actType in activityTypeCode.Split(','))
                {
                    // costruzione della stringa da processare
                    string line = string.Format(FIRST_INFO_STRING_FORMAT
                        , SEPARATOR
                        , deviceCode
                        , activityTypeCode//CommonService.AggiungiZeriASinistra(badgeCode, 10)
                        , regDateTime.Year
                        , regDateTime.Month.ToString("00")
                        , regDateTime.Day.ToString("00")
                        , regDateTime.Hour.ToString("00")
                        , regDateTime.Minute.ToString("00")
                        , " " //Reg direction per ora vuota
                        );
                    // aggiunta della stringa alla lista di scrittura
                    activityLines = line;
                }
                return activityLines;
            }
            internal static string CreateActivityLines(string deviceCode, string badgeCode, DateTime regDateTime, string activityTypeCode)
            {
                if (String.IsNullOrEmpty(activityTypeCode))
                    return "";

                string activityLines = "";

                foreach (string actType in activityTypeCode.Split(','))
                {
                    // costruzione della stringa da processare
                    string line = string.Format(EXTRA_INFO_STRING_FORMAT
                        , SEPARATOR
                        , deviceCode
                        , activityTypeCode//CommonService.AggiungiZeriASinistra(badgeCode, 10)
                        , regDateTime.Year
                        , regDateTime.Month.ToString("00")
                        , regDateTime.Day.ToString("00")
                        , regDateTime.Hour.ToString("00")
                        , regDateTime.Minute.ToString("00")
                        , " " //Reg direction per ora vuota
                        , INFOAGG
                        , TIPO_ATTIVITA
                        , actType
                        );
                    // aggiunta della stringa alla lista di scrittura
                    activityLines=line;
                }
                return activityLines;
            }
            internal static IEnumerable<string> CreatePruCodeActivityLines(string deviceCode, string badgeCode, DateTime regDateTime, string pruCodeForActivity)
            {
            if (string.IsNullOrEmpty(badgeCode)) {
                return null;
            }
                    

                // costruzione della stringa da processare
                string line = string.Format(EXTRA_INFO_STRING_FORMAT
                , SEPARATOR
                , deviceCode
                , CommonService.AggiungiZeriASinistra(badgeCode, 10)
                , regDateTime.Year
                , regDateTime.Month.ToString("00")
                , regDateTime.Day.ToString("00")
                , regDateTime.Hour.ToString("00")
                , regDateTime.Minute.ToString("00")
                , " " //Reg direction per ora vuota
                , INFOAGG //La keyword per capire che nella registrazione ci sono informazioni aggiuntive
                , PRUCODE // Segnalazione pru
                , pruCodeForActivity
                );
                // aggiunta della stringa alla lista di scrittura

                return new List<string>() { line };
            }

    }
}
