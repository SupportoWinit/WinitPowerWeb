using Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.BusinessServices.RegTranslatorService.Helpers
{
    internal static class ClockAppStringFormatter
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

        #endregion

        internal static IEnumerable<string> CreateGpsLines(string deviceCode, double latitude, double longitude, DateTime regDateTime)
        {
            string direzioneCardinaleLatitudine = "N";
            string direzioneCardinaleLongitudine = "E";
            string longitudeLine = "";
            string latitudeLine = "";

            if (latitude < 0)
                direzioneCardinaleLatitudine = "S";

            if (longitude < 0)
                direzioneCardinaleLongitudine = "O";

            if (latitude != 0 && longitude != 0) { 
                latitudeLine = string.Format(GPS_STRING_FORMAT
                    , SEPARATOR
                    , deviceCode
                    , CommonService.AggiungiZeriASinistra(latitude.ToString("00.0000000").Remove(2, 1), 10)
                    , regDateTime.Year
                    , regDateTime.Month.ToString("00")
                    , regDateTime.Day.ToString("00")
                    , regDateTime.Hour.ToString("00")
                    , regDateTime.Minute.ToString("00")
                    , NO_TAG_REFERENCE //se è solo GPS, non è tagReferenced, altrimenti sì
                    , MARKER_LATITUDINE
                    , direzioneCardinaleLatitudine
                    );

                longitudeLine = string.Format(GPS_STRING_FORMAT
                    , SEPARATOR
                    , deviceCode
                    , CommonService.AggiungiZeriASinistra(longitude.ToString("00.0000000").Remove(2, 1), 10)
                    , regDateTime.Year
                    , regDateTime.Month.ToString("00")
                    , regDateTime.Day.ToString("00")
                    , regDateTime.Hour.ToString("00")
                    , regDateTime.Minute.ToString("00")
                    , NO_TAG_REFERENCE //se è solo GPS, non è tagReferenced, altrimenti sì
                    , MARKER_LONGITUDINE
                    , direzioneCardinaleLongitudine
                    );
            }
            
            return new List<string>() { latitudeLine, longitudeLine };
        }

        internal static IEnumerable<string> CreateActivityLines(string deviceCode, string badgeCode, DateTime regDateTime, string activityTypeCode)
        {
            if (String.IsNullOrEmpty(activityTypeCode))
                return Enumerable.Empty<string>();

            List<string> activityLines = new List<string>();

            foreach (string actType in activityTypeCode.Split(','))
            {
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
                    , INFOAGG
                    , TIPO_ATTIVITA
                    , actType
                    );
                // aggiunta della stringa alla lista di scrittura
                activityLines.Add(line);
            }

            return activityLines;
        }

        internal static IEnumerable<string> CreatePruCodeActivityLines(string deviceCode, string badgeCode, DateTime regDateTime, string pruCodeForActivity)
        {
            if (string.IsNullOrEmpty(pruCodeForActivity))
                return Enumerable.Empty<string>();

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

        internal static IEnumerable<string> CreateNfcOrQrCodeLines(string deviceCode, string badgeCode, DateTime regDateTime, string direction)
        {
            if (String.IsNullOrEmpty(badgeCode))
                return new List<string>();

            string line = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}"
                , SEPARATOR
                , deviceCode
                , CommonService.AggiungiZeriASinistra(badgeCode, 10)
                , regDateTime.Year
                , regDateTime.Month.ToString("00")
                , regDateTime.Day.ToString("00")
                , regDateTime.Hour.ToString("00")
                , regDateTime.Minute.ToString("00")
                , direction
                );


            return new List<string>() { line };
        }

        internal static IEnumerable<string> CreateNfcLines(string deviceCode, string badgeCode, DateTime regDateTime)
        {
            string line = string.Format(NFC_QRCODE_STRING_FORMAT
                , SEPARATOR
                , deviceCode
                , CommonService.AggiungiZeriASinistra(badgeCode, 10)
                , regDateTime.Year
                , regDateTime.Month
                , regDateTime.Day
                , regDateTime.Hour
                , regDateTime.Minute
                , TAG_REFERENCE
                );


            return new List<string>() { line };
        }

        internal static IEnumerable<string> CreateTurnLines(string deviceCode, string badgeCode, DateTime regDateTime, string regDirection, string turnType)
        {
            if (String.IsNullOrEmpty(turnType))
                return Enumerable.Empty<string>();

            string turnLine = string.Format(EXTRA_INFO_STRING_FORMAT
                               , SEPARATOR
                               , deviceCode
                               , CommonService.AggiungiZeriASinistra(badgeCode, 10)
                               , regDateTime.Year
                               , regDateTime.Month
                               , regDateTime.Day
                               , regDateTime.Hour
                               , regDateTime.Minute
                               , regDirection
                               , INFOAGG
                               , TURNO
                               , turnType
                               );

            return new List<string>() { turnLine };
        }

        internal static IEnumerable<string> CreateSubCantLines(string deviceCode, string badgeCode, DateTime regDateTime, string regDirection, string codSottoCantiere, string descSottoCantiere)
        {
            if (String.IsNullOrEmpty(codSottoCantiere))
                return Enumerable.Empty<string>();

            string subCantLine = string.Format(EXTRA_INFO_STRING_FORMAT
                               , SEPARATOR
                               , deviceCode
                               , CommonService.AggiungiZeriASinistra(badgeCode, 10)
                               , regDateTime.Year
                               , regDateTime.Month.ToString("00")
                               , regDateTime.Day.ToString("00")
                               , regDateTime.Hour.ToString("00")
                               , regDateTime.Minute.ToString("00")
                               , regDirection
                               , INFOAGG
                               , SOTTOCANTIERE
                               , codSottoCantiere
                               , descSottoCantiere
                               );

            return new List<string>() { subCantLine };
        }

    }
}
