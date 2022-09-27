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

namespace Business.RegFileCreators
{
    class FlutterAppRegFileCreator : IRegFileCreator<FlutterAppReg>
    {
        string fileNamePatter;
        private static readonly int MARKER_LATITUDINE = 0;
        private static readonly int MARKER_LONGITUDINE = 1;
        private static readonly int TAG_REFERENCE = 1;
        private static readonly int NO_TAG_REFERENCE = 0;
        private static readonly string SEPARATOR = ";";
        private static readonly string INFOAGG = "INFOAGG";
        private static readonly string TIPO_ATTIVITA = "TIPOATT";

        private static readonly string EXTRA_INFO_STRING_FORMAT = "{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}";

        public FlutterAppRegFileCreator()
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

        public void WriteToFile(IEnumerable<FlutterAppReg> unEncodedRegs)
        {
            if (unEncodedRegs == null || !unEncodedRegs.Any())
                return;

            var regsToWrite = new List<string>();
            List<FlutterOrderedReg> regsToOrder = new List<FlutterOrderedReg>();

            foreach (var regs in unEncodedRegs)
            {
                FlutterOrderedReg var = new FlutterOrderedReg(regs.CodiceFru,regs.Value.First().CodicePru,regs.Value.First().Registrazione_Data_Ora_Orig,regs.Value.First().verso,regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine,regs.Value.First().Attivita,regs.Value.First().Squadra,regs.Value.First().Cantiere, regs.CreateDateTime,regs.hotspotTipo);
                regsToOrder.Add(var);
            }

            regsToOrder = regsToOrder.OrderBy(reg => reg.Dataord).ThenBy(reg => reg.CodicePru).ToList();
            String codGpsNfc = "";
            foreach (var fluReg in regsToOrder)
            {
                if (fluReg.Squadra != null && fluReg.Squadra != "")
                {
                    char separator = ',';
                    fluReg.Squadra = fluReg.Squadra.Replace("[", string.Empty);
                    fluReg.Squadra = fluReg.Squadra.Replace("]", string.Empty);
                    fluReg.Squadra = fluReg.Squadra.Trim();
                    String[] cols = fluReg.Squadra.Split(separator);
                    foreach (var badgeCode in cols)
                    {
                        string code = badgeCode.Trim();
                        fluReg.CodiceFru = code;

                        List<string> currentMemberLines = Translate(fluReg.CodiceFru, fluReg.CodicePru, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                        regsToWrite.AddRange(currentMemberLines);
                    }
                } else if (fluReg.Tecnologia == "2") {
                    if (codGpsNfc == "")
                    {
                        codGpsNfc = fluReg.CodiceFru;
                    }
                    else {
                        #region Creazione txt con registrazione NFC+GPS
                        if (fluReg.Cantiere.Length == 10)
                        {
                            string regRow = "";
                            if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0)
                            {
                                #region Reg con coordinate

                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                    codGpsNfc,
                                    AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    NO_TAG_REFERENCE,
                                    MARKER_LATITUDINE,
                                    "N"
                                );
                                regsToWrite.Add(regRow);
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                    codGpsNfc,
                                    AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    NO_TAG_REFERENCE,
                                    MARKER_LONGITUDINE,
                                    "E"
                                );

                                #endregion
                            }
                            else
                            {
                                #region Reg senza coordinate

                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                    codGpsNfc,
                                    fluReg.CodicePru,
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    fluReg.Verso
                                );
                                #endregion
                            }

                            regsToWrite.Add(regRow);
                            var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                            if (firstLine != "")
                            {
                                regsToWrite.Add(firstLine);
                            }
                            var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                            if (activityLine != "")
                            {
                                regsToWrite.Add(activityLine);
                            }
                            var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                            if (pruCodeAtivity != null)
                            {
                                regsToWrite.AddRange(pruCodeAtivity);
                            }
                        }
                        else
                        {
                            int cant = Int32.Parse(fluReg.CodicePru);
                            List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Cant_Id == cant).ToList();
                            string regRow = "";
                            if (cantiere.First().LatitudineGps_Can != 0 && cantiere.First().LatitudineGps_Can != 0)
                            {
                                #region Reg con coordinate

                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                    codGpsNfc,
                                    AggiungiZeriASinistra(cantiere.First().LatitudineGps_Can.ToString("00.0000000").Remove(2, 1), 10),
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    NO_TAG_REFERENCE,
                                    MARKER_LATITUDINE,
                                    "N"
                                );
                                regsToWrite.Add(regRow);
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                    codGpsNfc,
                                    AggiungiZeriASinistra(cantiere.First().LongitudineGps_Can.ToString("00.0000000").Remove(2, 1), 10),
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    NO_TAG_REFERENCE,
                                    MARKER_LONGITUDINE,
                                    "E"
                                );

                                #endregion
                            }
                            else
                            {
                                #region Reg senza coordinate
                                List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Cant_Id == cant).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                    codGpsNfc,
                                    matr.Last().Codice_Fru,
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    fluReg.Verso
                                );
                                #endregion
                            }

                            regsToWrite.Add(regRow);
                            var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                            if (firstLine != "")
                            {
                                regsToWrite.Add(firstLine);
                            }
                            var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                            if (activityLine != "")
                            {
                                regsToWrite.Add(activityLine);
                            }
                            var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                            if (pruCodeAtivity != null)
                            {
                                regsToWrite.AddRange(pruCodeAtivity);
                            }
                        }
                        #endregion
                        codGpsNfc = "";
                    }
                }
                else if (fluReg.Cantiere != null && fluReg.Cantiere != "") {
                    #region Creazione txt con registrazione manuale
                    if (fluReg.Cantiere.Length == 10)
                    {
                        string regRow = "";
                        if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0)
                        {
                            #region Reg con coordinate

                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                NO_TAG_REFERENCE,
                                MARKER_LATITUDINE,
                                "N"
                            );
                            regsToWrite.Add(regRow);
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                NO_TAG_REFERENCE,
                                MARKER_LONGITUDINE,
                                "E"
                            );

                            #endregion
                        }
                        else
                        {
                            #region Reg senza coordinate

                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                fluReg.CodiceFru,
                                fluReg.CodicePru,
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                fluReg.Verso
                            );
                            #endregion
                        }

                        regsToWrite.Add(regRow);
                        var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                        if (firstLine != "")
                        {
                            regsToWrite.Add(firstLine);
                        }
                        var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                        if (activityLine != "")
                        {
                            regsToWrite.Add(activityLine);
                        }
                        var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                        if (pruCodeAtivity != null)
                        {
                            regsToWrite.AddRange(pruCodeAtivity);
                        }
                    }
                    else {
                        int cant = Int32.Parse(fluReg.CodicePru);
                        List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Cant_Id == cant).ToList();
                        string regRow = "";
                        if (cantiere.First().LatitudineGps_Can != 0 && cantiere.First().LatitudineGps_Can != 0)
                        {
                            #region Reg con coordinate

                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(cantiere.First().LatitudineGps_Can.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                NO_TAG_REFERENCE,
                                MARKER_LATITUDINE,
                                "N"
                            );
                            regsToWrite.Add(regRow);
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(cantiere.First().LongitudineGps_Can.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                NO_TAG_REFERENCE,
                                MARKER_LONGITUDINE,
                                "E"
                            );

                            #endregion
                        }
                        else
                        {
                            #region Reg senza coordinate
                            List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Cant_Id == cant).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                            List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                fluReg.CodiceFru,
                                matr.Last().Codice_Fru,
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                fluReg.Verso
                            );
                            #endregion
                        }

                        regsToWrite.Add(regRow);
                        var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                        if (firstLine != "")
                        {
                            regsToWrite.Add(firstLine);
                        }
                        var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                        if (activityLine != "")
                        {
                            regsToWrite.Add(activityLine);
                        }
                        var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                        if (pruCodeAtivity != null)
                        {
                            regsToWrite.AddRange(pruCodeAtivity);
                        }
                    }
                    #endregion
                }
                else {
                    //String activityLines = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, fluReg.CodicePru, fluReg.Data, " ");
                    //if (activityLines != "")
                    //{
                    //    regsToWrite.Add(activityLines);
                    //}

                    string regRow = "";
                    if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0)
                    {
                        #region Reg con coordinate

                        regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                            fluReg.CodiceFru,
                            AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                            fluReg.Data.Year,
                            fluReg.Data.Month.ToString("00"),
                            fluReg.Data.Day.ToString("00"),
                            fluReg.Data.Hour.ToString("00"),
                            fluReg.Data.Minute.ToString("00"),
                            NO_TAG_REFERENCE,
                            MARKER_LATITUDINE,
                            "N"
                        );
                        regsToWrite.Add(regRow);
                        regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                            fluReg.CodiceFru,
                            AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                            fluReg.Data.Year,
                            fluReg.Data.Month.ToString("00"),
                            fluReg.Data.Day.ToString("00"),
                            fluReg.Data.Hour.ToString("00"),
                            fluReg.Data.Minute.ToString("00"),
                            NO_TAG_REFERENCE,
                            MARKER_LONGITUDINE,
                            "E"
                        );

                        #endregion
                    }
                    else
                    {
                        #region Reg senza coordinate

                        regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                            fluReg.CodiceFru,
                            fluReg.CodicePru,
                            fluReg.Data.Year,
                            fluReg.Data.Month.ToString("00"),
                            fluReg.Data.Day.ToString("00"),
                            fluReg.Data.Hour.ToString("00"),
                            fluReg.Data.Minute.ToString("00"),
                            fluReg.Verso
                        );
                        #endregion
                    }

                    regsToWrite.Add(regRow);
                    var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                    if (firstLine != "") {
                        regsToWrite.Add(firstLine);
                    }
                    var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Attivita);
                    if (activityLine != "")
                    {
                        regsToWrite.Add(activityLine);
                    }
                    var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                    if (pruCodeAtivity != null)
                    {
                        regsToWrite.AddRange(pruCodeAtivity);
                    }
                }
                
            }

            File.WriteAllLines(fileNamePatter, regsToWrite.ToArray());
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
                    codiceFru,
                    codicePru,
                    Data.Year,
                    Data.Month.ToString("00"),
                    Data.Day.ToString("00"),
                    Data.Hour.ToString("00"),
                    Data.Minute.ToString("00"),
                    Verso
                );
                translated.Add(regRow);
                #endregion
            }
            return translated;
        }
    }
}
