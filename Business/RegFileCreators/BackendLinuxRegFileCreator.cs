using Business.DataClasses.FlutterAppDTOs;
using System;
using System.Collections.Generic;
using System.IO;
using Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Business.BusinessServices.RegTranslatorService.Helpers;
using Business.Repository;
using Domain;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using Business.Profile;
using Common;
using Business.DataClasses.BackendNewDTOs;

namespace Business.RegFileCreators
{
    class BackendLinuxRegFileCreator : IRegFileCreator<BackendNewReg>
    {
        string fileNamePatter;
        private static readonly int MARKER_LATITUDINE = 0;
        private static readonly int MARKER_LONGITUDINE = 1;
        private static readonly int TAG_REFERENCE = 1;
        private static readonly int NO_TAG_REFERENCE = 0;
        private static readonly string SEPARATOR = ";";
        private static readonly string INFOAGG = "INFOAGG";
        private static readonly string TIPO_ATTIVITA = "TIPOATT";
        private static readonly string NOTE = "NOTE";

        private static readonly string EXTRA_INFO_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}";

        public BackendLinuxRegFileCreator()
        {
            fileNamePatter = String.Format("{0}{1}_{2}-{3}-{4}_{5}-{6}-{7}.txt",
                HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path),
                Common.Properties.Settings.Default.RegFile,
                DateTime.Now.Year,
                DateTime.Now.Month.ToString("00"),
                DateTime.Now.Day.ToString("00"),
                DateTime.Now.Hour.ToString("00"),
                DateTime.Now.Minute.ToString("00"),
                DateTime.Now.Second.ToString("00")
                );
        }

        public void WriteToFile(IEnumerable<BackendNewReg> unEncodedRegs)
        {
            if ((unEncodedRegs == null || !unEncodedRegs.Any()))
                return;

            unEncodedRegs = unEncodedRegs.OrderBy(reg => reg.CodiceFru).ThenBy(reg => reg.DataRegistrazione).ToList();
            var regsByCol = unEncodedRegs.GroupBy(reg => reg.CodiceFru);

            String codGpsNfc = "";
            var regsToWrite = new List<string>();
            BackendNewReg temp = unEncodedRegs.First();
            BackendNewReg last = unEncodedRegs.First();
            bool lastCoordinate = false;
            string regRowGpsNfc = "";
            string lastLatitudeTxt = "";
            string lastLongitudeTxt = "";
            string lastCodiceFru = "";
            string lastAtt = "";
            DateTime lastData = new DateTime(1900, 01, 01);
            foreach (var regs in regsByCol)
            {
                foreach (var regsByDate in regs.GroupBy(reg => reg.DataRegistrazione))
                {
                    foreach (var fluReg in regsByDate)
                    {
                        #region Creazione txt normale
                        //vado a creare il txt per una semplice registrazione GPS,NFC o QrCode
                        string regRow = "";
                        if (fluReg.CodiceNFC != "" && !string.IsNullOrEmpty(fluReg.CodiceNFC))
                        {
                            #region Reg senza coordinate
                            string verso = "";
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                                fluReg.CodiceFru,
                                fluReg.CodiceNFC,
                                fluReg.DataRegistrazione.Year,
                                fluReg.DataRegistrazione.Month.ToString("00"),
                                fluReg.DataRegistrazione.Day.ToString("00"),
                                fluReg.DataRegistrazione.Hour.ToString("00"),
                                fluReg.DataRegistrazione.Minute.ToString("00"),
                                fluReg.DataRegistrazione.Second.ToString("00"),
                                verso
                            );
                            #endregion

                            regsToWrite.Add(regRow);
                            #endregion
                            last = fluReg;
                        }
                    }
                }
                File.WriteAllLines(fileNamePatter, regsToWrite.ToArray());
            }
        }

        public Cant_Note getLastAtt(List<Cant_Note> lista, DateTime oggi) 
        {
            Cant_Note returnValue = default(Cant_Note);
            Cant_Note lastCnt = default(Cant_Note);
            DateTime lastDate = default(DateTime);
            foreach (Cant_Note cnt in lista)
            {
                if (oggi >= cnt.Data_Nota_Can_Note)
                {
                    //se la data di oggi è maggiore della data di associazione controllo se l'ultima data è default
                    if (lastDate == default(DateTime))
                    {
                        if (cnt == lista.OrderByDescending(fr => fr.Data_Nota_Can_Note).FirstOrDefault())
                        {
                            //se è l'ultima della lista associo l'elemento corrente alla variabile finale
                            returnValue = cnt;
                        }
                        else
                        {
                            //se è default vuol dire che è la prima associazione, associo all'ultima data la prima data d'associazione
                            lastDate = cnt.Data_Nota_Can_Note;
                            lastCnt = cnt;
                        }
                    }
                    else
                    {
                        //se non è default vuol dire che la data di oggi è più grande dell'associazione precedente
                        if (oggi >= cnt.Data_Nota_Can_Note)
                        {
                            //se la data di oggi è maggiore a quella d'associazione controllo se è l'ultimo della lista
                            if (cnt == lista.OrderByDescending(fr => fr.Data_Nota_Can_Note).FirstOrDefault())
                            {
                                //se è l'ultima della lista associo l'elemento corrente alla variabile finale
                                returnValue = cnt;
                            }
                            else
                            {
                                //se non è l'ultimo elemento aggiorno le variabili temporanee
                                lastDate = cnt.Data_Nota_Can_Note;
                                lastCnt = cnt;
                            }
                        }
                        else
                        {
                            //se non è maggiore della data dell'associazione attuale metto nella associazione quella preedente
                            returnValue = lastCnt;
                        }
                    }
                }
                else 
                {
                    returnValue = lastCnt;
                }
            }

            return returnValue;
        }
        
        public static string AggiungiZeriASinistra(string sStringa, int iLunghezzaStringa)
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return CompletaASinistra(sStringa, iLunghezzaStringa, '0');
        }
        static public string CompletaASinistra(string sStringa, int iLunghezzaStringa, char completatore = ' ')
        {
            if (string.IsNullOrEmpty(sStringa)) return null;
            return sStringa.PadLeft(iLunghezzaStringa, completatore);
        }

        //metodo che serve nel caso in cui la timbratura sia di squadra
        static public List<string> Translate(string codiceFru,string codicePru,DateTime Data, string Verso, string Motivazione, double latitudine, double longitudine) {
            List<string> translated = new List<string>();
            string regRow = "";
            if (latitudine != 0 && longitudine != 0)
            {
                #region Reg con coordinate

                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                    codiceFru,
                    AggiungiZeriASinistra(latitudine.ToString("00.0000000").Remove(2, 1), 10),
                    Data.Year,
                    Data.Month.ToString("00"),
                    Data.Day.ToString("00"),
                    Data.Hour.ToString("00"),
                    Data.Minute.ToString("00"),
                    NO_TAG_REFERENCE,
                    MARKER_LATITUDINE,
                    "N"
                );
                translated.Add(regRow);
                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                    codiceFru,
                    AggiungiZeriASinistra(longitudine.ToString("00.0000000").Remove(2, 1), 10),
                    Data.Year,
                    Data.Month.ToString("00"),
                    Data.Day.ToString("00"),
                    Data.Hour.ToString("00"),
                    Data.Minute.ToString("00"),
                    NO_TAG_REFERENCE,
                    MARKER_LONGITUDINE,
                    "E"
                );
                translated.Add(regRow);
                #endregion
            }
            else
            {
                #region Reg senza coordinate

                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                    codicePru,
                    codiceFru,
                    Data.Year,
                    Data.Month.ToString("00"),
                    Data.Day.ToString("00"),
                    Data.Hour.ToString("00"),
                    Data.Minute.ToString("00"),
                    " ",
                    Motivazione != "" ? "[Motivazione]=" + Motivazione : null
                );
                translated.Add(regRow);
                #endregion
            }
            return translated;
        }

        public void WriteToFile(IEnumerable<BackendNewReg> unEncodedRegs, IEnumerable<FlutterAppRegOld> unEncodedRegsOld, IEnumerable<BackendNewReg> unEncodedRegs2)
        {
            throw new NotImplementedException();
        }
    }
}
