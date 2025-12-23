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
        private static readonly string NOTE = "NOTE";

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

        public void WriteToFile(IEnumerable<FlutterAppReg> unEncodedRegs, IEnumerable<FlutterAppRegOld> unEncodedRegsOld, IEnumerable<FlutterAppReg> unEncodedRegs2)
        {
            if ((unEncodedRegs == null || !unEncodedRegs.Any()) && (unEncodedRegsOld == null || !unEncodedRegsOld.Any()) && (unEncodedRegs2 == null || !unEncodedRegs2.Any()))
                return;

            var regsToWrite = new List<string>();
            List<FlutterOrderedReg> regsToOrder = new List<FlutterOrderedReg>();
            int gpsnfc = 0;
            //La lista è valorizzata se hanno el app sul nuovo backend
            if (unEncodedRegs != null) {
                foreach (var regs in unEncodedRegs)
                {
                    FlutterOrderedReg var = null;
                    if (regs.Value != null) {
                        if (regs.Value.First().CodicePru != "")
                        {
                            var = new FlutterOrderedReg(regs.CodiceFru, regs.Value.First().CodicePru, regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                        }
                        else
                        {
                            var = new FlutterOrderedReg(regs.CodiceFru, "", regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                        }

                        regsToOrder.Add(var);
                    } 
                }
            }
            //La lista è valorizzata se hanno timbrature sul vecchio backend
            if (unEncodedRegs2 != null) {
                foreach (var regs in unEncodedRegs2)
                {
                    FlutterOrderedReg var = null;
                    if (regs.Value.First().CodicePru != "")
                    {
                        var = new FlutterOrderedReg(regs.CodiceFru, regs.Value.First().CodicePru, regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                    }
                    else
                    {
                        var = new FlutterOrderedReg(regs.CodiceFru, "", regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                    }

                    regsToOrder.Add(var);
                }
            }
            //Questa lista viene utilizzata nel caso in cui si abbiano più istanze della nuova app per un cliente
            if (unEncodedRegsOld != null) {
                foreach (var regs in unEncodedRegsOld)
                {
                    FlutterOrderedReg var = null;
                    if (regs.Value.First().CodicePru != "")
                    {
                        var = new FlutterOrderedReg(regs.CodiceFru, regs.Value.First().CodicePru, regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                    }
                    else
                    {
                        var = new FlutterOrderedReg(regs.CodiceFru, "", regs.Value.First().Registrazione_Data_Ora_Orig, regs.Value.First().verso, regs.Value.First().motivazione, regs.Value.First().Latitudine, regs.Value.First().Longitudine, regs.Value.First().Attivita, regs.Value.First().Squadra, regs.Value.First().Cantiere, regs.Value.First().NfcGps, regs.CreateDateTime, regs.hotspotTipo, regs.Value.First().Note, regs.Value.First().Chiave, regs.Value.First().CantiereSel);
                    }

                    regsToOrder.Add(var);
                }
            }        
            regsToOrder = regsToOrder.OrderBy(reg => reg.CodiceFru).ThenBy(reg => reg.Dataord).ToList();
            var regsByCol = regsToOrder.GroupBy(reg => reg.CodiceFru);
          
            String codGpsNfc = "";
            FlutterOrderedReg temp = regsToOrder.First();
            FlutterOrderedReg last = regsToOrder.First();
            bool lastCoordinate = false;
            string regRowGpsNfc = "";
            string lastLatitudeTxt = "";
            string lastLongitudeTxt = "";
            string lastCodiceFru = "";
            string lastAtt = "";
            double lastLatitude = 0.0;
            double lastLongitude = 0.0;
            FlutterOrderedReg tmpLastReg = null;
            DateTime lastData = new DateTime(1900,01,01);
              foreach (var regs in regsByCol)
            {
                foreach (var regsByDate in regs.GroupBy(reg => reg.Data)) {
                    foreach (var fluReg in regsByDate) {
                        if (fluReg.Squadra != null && fluReg.Squadra != "")
                        {
                            #region Creazione txt per una timbratura con la squadra
                            //vado ad estrarre tutti i collaboratori per i quali è stata fatta la timbratura di squadra
                            char separator = ',';
                            fluReg.Squadra = fluReg.Squadra.Replace("[", string.Empty);
                            fluReg.Squadra = fluReg.Squadra.Replace("]", string.Empty);
                            fluReg.Squadra = fluReg.Squadra.Trim();
                            String[] cols = fluReg.Squadra.Split(separator);
                            List<string> lastReg = new List<string>();
                            if (fluReg.Motivazione == "E" || fluReg.Motivazione == "U")
                            {
                                fluReg.Motivazione = "";
                            }
                            if (fluReg.Cantiere != null && fluReg.Cantiere != "")
                            {
                                if (fluReg.Cantiere.Length == 10)
                                {
                                    lastReg = Translate(fluReg.CodiceFru, fluReg.Cantiere, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                                }
                                else
                                {
                                    if (fluReg.Cantiere != "Non selezionato")
                                    {
                                        if (fluReg.CodicePru != "" && fluReg.CodicePru != "Non selezionato")
                                        {
                                            int cant = Int32.Parse(fluReg.CodicePru);
                                            List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Cant_Id == cant).ToList();
                                            if (cantiere.First().LatitudineGps_Can != 0 && cantiere.First().LatitudineGps_Can != 0)
                                            {
                                                #region Reg con coordinate
                                                lastReg = Translate(fluReg.CodiceFru, "", fluReg.Data, "", fluReg.Motivazione, cantiere.First().LatitudineGps_Can, cantiere.First().LongitudineGps_Can);
                                                #endregion
                                            }
                                            else
                                            {
                                                #region Reg senza coordinate
                                                //vado a prelevare l a matricola associata e la inserisco nel txt
                                                List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Cant_Id == cant).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                                                lastReg = Translate(fluReg.CodiceFru, matr.Last().Codice_Fru, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                                                #endregion
                                            }
                                        }
                                        else
                                        {
                                            //int cant = Int32.Parse(fluReg.CodicePru);
                                            List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Descrizione_Can.Equals(fluReg.Cantiere)).ToList();
                                            if (cantiere.Count > 0)
                                            {
                                                int i;
                                                if (cantiere.First().LatitudineGps_Can != 0 && cantiere.First().LatitudineGps_Can != 0)
                                                {
                                                    #region Reg con coordinate
                                                    lastReg = Translate(fluReg.CodiceFru, "", fluReg.Data, "", fluReg.Motivazione, cantiere.First().LatitudineGps_Can, cantiere.First().LongitudineGps_Can);
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Reg senza coordinate
                                                    //vado a prelevare l a matricola associata e la inserisco nel txt
                                                    List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Cant_Id == cantiere.First().Cant_Id).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                    List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                                                    lastReg = Translate(fluReg.CodiceFru, matr.Last().Codice_Fru, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                                                    #endregion
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                            else
                            {
                                lastReg = Translate(fluReg.CodiceFru, fluReg.CodicePru, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                            }
                            regsToWrite.AddRange(lastReg);
                            //tutte le operazioni precedenti sono per estrarre solo le matricole dei collaboratori
                            foreach (var badgeCode in cols)
                            {
                                string code = badgeCode.Trim();
                                fluReg.CodiceFru = code;
                                List<string> currentMemberLines = new List<string>();
                                if (fluReg.Cantiere != null && fluReg.Cantiere != "")
                                {
                                    if (fluReg.Cantiere.Length == 10)
                                    {
                                        currentMemberLines = Translate(fluReg.CodiceFru, fluReg.Cantiere, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                                    }
                                    else
                                    {
                                        if (fluReg.Cantiere != "Non selezionato")
                                        {
                                            if (fluReg.CodicePru != "" && fluReg.CodicePru != "Non selezionato")
                                            {
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
                                                    regsToWrite.Add(regRow);
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Reg senza coordinate
                                                    //vado a prelevare l a matricola associata e la inserisco nel txt
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
                                                        ""
                                                    );
                                                    #endregion
                                                }
                                            }
                                            else
                                            {
                                                List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Descrizione_Can == fluReg.Cantiere).ToList();
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
                                                    regsToWrite.Add(regRow);
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Reg senza coordinate
                                                    //vado a prelevare l a matricola associata e la inserisco nel txt
                                                    List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Descrizione_Can == fluReg.Cantiere).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                    List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                                        fluReg.CodiceFru,
                                                        matr.Last().Codice_Fru,
                                                        fluReg.Data.Year,
                                                        fluReg.Data.Month.ToString("00"),
                                                        fluReg.Data.Day.ToString("00"),
                                                        fluReg.Data.Hour.ToString("00"),
                                                        fluReg.Data.Minute.ToString("00"),
                                                        ""
                                                    );
                                                    #endregion
                                                }
                                            }

                                        }
                                    }

                                }
                                else
                                {
                                    currentMemberLines = Translate(fluReg.CodiceFru, fluReg.CodicePru, fluReg.Data, "", fluReg.Motivazione, fluReg.Latitudine, fluReg.Longitudine);
                                }
                                //pulisco la stringa e in seguito ciclo per ogni matricola così da fare una timbratura per collaboratore

                                regsToWrite.AddRange(currentMemberLines);
                            }
                            #endregion
                        }
                        else if ((fluReg.NfcGps != null && fluReg.NfcGps == "1") || gpsnfc == 1)
                        {
                            #region Creazione txt con registrazione NFC+GPS
                            //controllo se la prima timbratura NFC/GPS era con le coordinate, nel caso è errato e devo fare altre azioni
                            if (lastCoordinate == true && gpsnfc == 1)
                            {
                                //eseguo il txt con i criteri necessari per identificare la timbratura come NFC+GPS
                                if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0)
                                {
                                    #region Reg con coordinate

                                    regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        MARKER_LATITUDINE,
                                        "N"
                                    );
                                    regsToWrite.Add(regRowGpsNfc);
                                    regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        MARKER_LONGITUDINE,
                                        "E"
                                    );
                                    lastCoordinate = true;
                                    #endregion
                                }
                                else if (fluReg.CodicePru != "")
                                {
                                    #region Reg senza coordinate

                                    regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};",
                                        fluReg.CodiceFru,
                                        fluReg.CodicePru,
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        " "
                                    );
                                    #endregion
                                }

                                regsToWrite.Add(regRowGpsNfc);
                                var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(codGpsNfc, "", fluReg.Data, fluReg.Attivita);
                                if (firstLine != "")
                                {
                                    regsToWrite.Add(firstLine);
                                }
                                var activityLine = FlutterAppStringFormatter.CreateActivityLines(codGpsNfc, "", fluReg.Data, fluReg.Attivita);
                                if (activityLine != "")
                                {
                                    regsToWrite.Add(activityLine);
                                }
                                var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(codGpsNfc, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                                if (pruCodeAtivity != null)
                                {
                                    regsToWrite.AddRange(pruCodeAtivity);
                                }

                                //una volta inserita la timbratura NFC procedo ad inserire la timbratura GPS che in maniera errata si trovava come prima timbratura del gruppo
                                regsToWrite.Add(lastLatitudeTxt);
                                regsToWrite.Add(lastLongitudeTxt);
                                firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(codGpsNfc, "", lastData, lastAtt);
                                if (firstLine != "")
                                {
                                    regsToWrite.Add(firstLine);
                                }
                                activityLine = FlutterAppStringFormatter.CreateActivityLines(codGpsNfc, "", lastData, lastAtt);
                                if (activityLine != "")
                                {
                                    regsToWrite.Add(activityLine);
                                }
                                pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(codGpsNfc, lastAtt, lastData, lastCodiceFru);
                                if (pruCodeAtivity != null)
                                {
                                    regsToWrite.AddRange(pruCodeAtivity);
                                }
                                if (gpsnfc == 0)
                                {
                                    gpsnfc = 1;
                                }
                                else
                                {
                                    lastCoordinate = false;
                                    gpsnfc = 0;
                                }
                                regRowGpsNfc = "";
                            }
                            else
                            {
                                //eseguo il txt con i criteri necessari per identificare la timbratura come NFC+GPS
                                if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0)
                                {
                                    #region Reg con coordinate
                                    //controllo se è la prima timbratura del gruppo GPS/NFC, se è così imposto il flag e mi salvo le registrazione nelle variabili temporanee
                                    if (gpsnfc == 0)
                                    {
                                        lastCoordinate = true;

                                        regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        MARKER_LATITUDINE,
                                        "N"
                                        );
                                        lastLatitudeTxt = regRowGpsNfc;
                                        regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                            fluReg.CodiceFru,
                                            AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                            fluReg.Data.Year,
                                            fluReg.Data.Month.ToString("00"),
                                            fluReg.Data.Day.ToString("00"),
                                            fluReg.Data.Hour.ToString("00"),
                                            fluReg.Data.Minute.ToString("00"),
                                            fluReg.Data.Second.ToString("00"),
                                            TAG_REFERENCE,
                                            MARKER_LONGITUDINE,
                                            "E"
                                        );
                                        lastLongitudeTxt = regRowGpsNfc;
                                    }
                                    else
                                    {
                                        regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        MARKER_LATITUDINE,
                                        "N"
                                        );
                                        regsToWrite.Add(regRowGpsNfc);
                                        regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                            fluReg.CodiceFru,
                                            AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                            fluReg.Data.Year,
                                            fluReg.Data.Month.ToString("00"),
                                            fluReg.Data.Day.ToString("00"),
                                            fluReg.Data.Hour.ToString("00"),
                                            fluReg.Data.Minute.ToString("00"),
                                            fluReg.Data.Second.ToString("00"),
                                            TAG_REFERENCE,
                                            MARKER_LONGITUDINE,
                                            "E"
                                        );
                                    }
                                    #endregion
                                }
                                else if (fluReg.CodicePru != "")
                                {
                                    #region Reg senza coordinate

                                    regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};",
                                        fluReg.CodiceFru,
                                        fluReg.CodicePru,
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        TAG_REFERENCE,
                                        " "
                                    );
                                    #endregion
                                }
                                //nel caso sia la prima timbratura del gruppo GPS/NFC e sono coordinate mi salvo il tutto in variabili temporanee
                                if (gpsnfc == 0 && lastCoordinate)
                                {
                                    lastAtt = fluReg.Attivita;
                                    lastData = fluReg.Data;
                                    lastCodiceFru = fluReg.CodiceFru;
                                    gpsnfc = 1;
                                }
                                else
                                {
                                    regsToWrite.Add(regRowGpsNfc);
                                    var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(codGpsNfc, "", fluReg.Data, fluReg.Attivita);
                                    if (firstLine != "")
                                    {
                                        regsToWrite.Add(firstLine);
                                    }
                                    var activityLine = FlutterAppStringFormatter.CreateActivityLines(codGpsNfc, "", fluReg.Data, fluReg.Attivita);
                                    if (activityLine != "")
                                    {
                                        regsToWrite.Add(activityLine);
                                    }
                                    var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(codGpsNfc, fluReg.Attivita, fluReg.Data, fluReg.CodiceFru);
                                    if (pruCodeAtivity != null)
                                    {
                                        regsToWrite.AddRange(pruCodeAtivity);
                                    }
                                    //se è la prima timbratura del gruppo me lo segno
                                    //in caso contrario resetto le variabili collegato al controllo
                                    if (gpsnfc == 0)
                                    {
                                        gpsnfc = 1;
                                    }
                                    else
                                    {
                                        lastCoordinate = false;
                                        gpsnfc = 0;
                                    }
                                    regRowGpsNfc = "";
                                }
                            }
                            #endregion
                        }
                        else if (fluReg.NfcGps != null && (fluReg.NfcGps.StartsWith("WIN") || fluReg.NfcGps.StartsWith("CA")))
                        {
                            #region Creazione txt con registrazione NFC+GPS

                            #region Reg senza coordinate

                            regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};",
                                fluReg.CodiceFru,
                                fluReg.NfcGps,
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                fluReg.Data.Second.ToString("00"),
                                TAG_REFERENCE,
                                " "
                            );
                            regsToWrite.Add(regRowGpsNfc);
                            #endregion

                            #region Reg con coordinate

                            regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                fluReg.Data.Second.ToString("00"),
                                TAG_REFERENCE,
                                MARKER_LATITUDINE,
                                "N"
                            );
                            regsToWrite.Add(regRowGpsNfc);
                            regRowGpsNfc = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                                fluReg.CodiceFru,
                                AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                fluReg.Data.Second.ToString("00"),
                                TAG_REFERENCE,
                                MARKER_LONGITUDINE,
                                "E"
                            );
                            lastCoordinate = true;
                            regsToWrite.Add(regRowGpsNfc);
                            #endregion

                            var noteLine = "";
                            if (fluReg.CodicePru != "" && !string.IsNullOrEmpty(fluReg.CodicePru))
                            {
                                noteLine = FlutterAppStringFormatter.CreateNoteLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            }
                            else
                            {
                                noteLine = FlutterAppStringFormatter.CreateNoteGpsLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            }
                            if (noteLine != "")
                            {
                                regsToWrite.Add(noteLine);
                            }
                            String attivitaFInale = "";
                            if (fluReg.Attivita.Contains(','))
                            {
                                String[] att = fluReg.Attivita.Split(',');
                                attivitaFInale = att[0];
                            }
                            else
                            {
                                attivitaFInale = fluReg.Attivita;
                            }
                            if (attivitaFInale == "none")
                            {
                                attivitaFInale = "";
                            }
                            var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, attivitaFInale);
                            if (firstLine != "")
                            {
                                regsToWrite.Add(firstLine);
                            }
                            var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, attivitaFInale);
                            if (activityLine != "")
                            {
                                regsToWrite.Add(activityLine);
                            }
                            var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, attivitaFInale, fluReg.Data, fluReg.CodiceFru);
                            if (pruCodeAtivity != null)
                            {
                                regsToWrite.AddRange(pruCodeAtivity);
                            }
                            #endregion
                        }
                        else if (fluReg.Cantiere != null && fluReg.Cantiere != "")
                        {
                            #region Creazione txt con registrazione manuale
                            //vado a controllare se è arrivato il tag (il cantiere è stato inserito a mano nel db) oppure è arrivato l'id (cantiere inserito tramite api PW)
                            if (fluReg.Cantiere.Length == 10)
                            {
                                //nel caso sia arrivato il tag basterà andare ad inserirlo nel txt
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

                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                                        fluReg.CodiceFru,
                                        fluReg.Cantiere,
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        "",
                                        fluReg.Motivazione != "" ? "[Motivazione]=" + fluReg.Motivazione : null
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
                                if (fluReg.Cantiere != "Non selezionato")
                                {
                                    //nel caso sia arrivato l'id del cantiere vado a ricercare la matricola associata al cantiere con l'id che ci è arrivato
                                    string regRow = "";
                                    if (fluReg.CodicePru != "")
                                    {
                                        int cant = Int32.Parse(fluReg.CodicePru);
                                        List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Cant_Id == cant).ToList();

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
                                            //vado a prelevare l a matricola associata e la inserisco nel txt
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
                                                ""
                                            );
                                            #endregion
                                        }
                                    }
                                    else
                                    {
                                        List<Cant> cantiere = RepoManager.CantRepo.GetAll().Where(can => can.Descrizione_Can == fluReg.Cantiere).ToList();
                                        if (cantiere.Count > 0)
                                        {
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
                                                //vado a prelevare l a matricola associata e la inserisco nel txt
                                                List<Fru_Cant> fru = RepoManager.Fru_CantRepo.GetAll().Where(can => can.Cant_Id == cantiere.First().Cant_Id).OrderBy(can => can.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                List<Fru> matr = RepoManager.FruRepo.GetAll().Where(can => can.Fru_Id == fru.First().Fru_Id).ToList();
                                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                                                    fluReg.CodiceFru,
                                                    matr.Last().Codice_Fru,
                                                    fluReg.Data.Year,
                                                    fluReg.Data.Month.ToString("00"),
                                                    fluReg.Data.Day.ToString("00"),
                                                    fluReg.Data.Hour.ToString("00"),
                                                    fluReg.Data.Minute.ToString("00"),
                                                    ""
                                                );
                                                #endregion
                                            }
                                        }
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
                            }
                            #endregion
                        }
                        else if (fluReg.Chiave != null && fluReg.Chiave != "")
                        {
                            #region Reg senza coordinate
                            string regRow = "";
                            string verso = "";
                            if (fluReg.Motivazione == "E" || fluReg.Motivazione == "U")
                            {
                                verso = fluReg.Motivazione;
                                fluReg.Motivazione = "";
                            }
                            if (fluReg.Motivazione == "none")
                            {
                                fluReg.Motivazione = "";
                            }
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                                fluReg.Chiave,
                                fluReg.CodicePru,
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                verso,
                                fluReg.Motivazione != "" ? "[Motivazione]=" + fluReg.Motivazione : null
                            );
                            regsToWrite.Add(regRow);
                            var noteLine = FlutterAppStringFormatter.CreateNoteLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            if (noteLine != "")
                            {
                                regsToWrite.Add(noteLine);
                            }
                            #endregion
                        }
                        else if (fluReg.CantiereSel != null && fluReg.CantiereSel != "")
                        {
                            #region Reg senza coordinate
                            string regRow = "";
                            string verso = "";
                            if (fluReg.Motivazione == "E" || fluReg.Motivazione == "U")
                            {
                                verso = fluReg.Motivazione;
                                fluReg.Motivazione = "";
                            }
                            if (fluReg.Motivazione == "none")
                            {
                                fluReg.Motivazione = "";
                            }
                            regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                                fluReg.CantiereSel,
                                fluReg.CodicePru,
                                fluReg.Data.Year,
                                fluReg.Data.Month.ToString("00"),
                                fluReg.Data.Day.ToString("00"),
                                fluReg.Data.Hour.ToString("00"),
                                fluReg.Data.Minute.ToString("00"),
                                verso,
                                fluReg.Motivazione != "" ? "[Motivazione]=" + fluReg.Motivazione : null
                            );
                            regsToWrite.Add(regRow);
                            var noteLine = FlutterAppStringFormatter.CreateNoteLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            if (noteLine != "")
                            {
                                regsToWrite.Add(noteLine);
                            }
                            #endregion
                        }
                        else
                        {
                            #region Creazione txt normale
                            //vado a creare il txt per una semplice registrazione GPS,NFC o QrCode
                            string regRow = "";
                            if ((string.IsNullOrEmpty(last.CodicePru) && last.Latitudine == 0 && last.Longitudine == 0) && (fluReg.Motivazione == "Pausa" && fluReg.Verso == "U"))
                            {
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",
                                        last.CodiceFru,
                                        "PAUSA00001",
                                        last.Data.Year,
                                        last.Data.Month.ToString("00"),
                                        last.Data.Day.ToString("00"),
                                        last.Data.Hour.ToString("00"),
                                        last.Data.Minute.ToString("00"),
                                        last.Data.Second.ToString("00"),
                                        "E",
                                        "[Motivazione]=Pausa"
                                    );
                                regsToWrite.Add(regRow);
                                var firstLine1 = FlutterAppStringFormatter.CreateFirstActivityLines(last.CodiceFru, "", last.Data, last.Attivita);
                                if (firstLine1 != "")
                                {
                                    regsToWrite.Add(firstLine1);
                                }
                                var activityLine1 = FlutterAppStringFormatter.CreateActivityLines(last.CodiceFru, "", last.Data, last.Attivita);
                                if (activityLine1 != "")
                                {
                                    regsToWrite.Add(activityLine1);
                                }
                                var pruCodeAtivity1 = FlutterAppStringFormatter.CreatePruCodeActivityLines(last.CodiceFru, last.Attivita, last.Data, last.CodiceFru);
                                if (pruCodeAtivity1 != null)
                                {
                                    regsToWrite.AddRange(pruCodeAtivity1);
                                }
                            }
                            if (fluReg.Latitudine != 0 && fluReg.Longitudine != 0 && !string.IsNullOrEmpty(fluReg.Latitudine.ToString()))
                            {
                                string verso = "";
                                if (fluReg.Motivazione == "E" || fluReg.Motivazione == "U")
                                {
                                    verso = fluReg.Motivazione;
                                    fluReg.Motivazione = "";
                                }
                                else
                                {
                                    verso = fluReg.Verso;
                                }
                                #region Reg con coordinate
                                if (fluReg.Latitudine < 0 && fluReg.Longitudine < 0)
                                {
                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};;",
                                    fluReg.CodiceFru,
                                    AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(3, 1), 10),
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    fluReg.Data.Second.ToString("00"),
                                    NO_TAG_REFERENCE,
                                    MARKER_LATITUDINE,
                                    "N"
                                    );
                                    regsToWrite.Add(regRow);
                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};;",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(3, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        NO_TAG_REFERENCE,
                                        MARKER_LONGITUDINE,
                                        "E"
                                    );
                                }
                                else
                                {
                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};;",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Latitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        NO_TAG_REFERENCE,
                                        MARKER_LATITUDINE,
                                        "N"
                                    );
                                    regsToWrite.Add(regRow);
                                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};;",
                                        fluReg.CodiceFru,
                                        AggiungiZeriASinistra(fluReg.Longitudine.ToString("00.0000000").Remove(2, 1), 10),
                                        fluReg.Data.Year,
                                        fluReg.Data.Month.ToString("00"),
                                        fluReg.Data.Day.ToString("00"),
                                        fluReg.Data.Hour.ToString("00"),
                                        fluReg.Data.Minute.ToString("00"),
                                        fluReg.Data.Second.ToString("00"),
                                        NO_TAG_REFERENCE,
                                        MARKER_LONGITUDINE,
                                        "E"
                                    );
                                }
                                #endregion
                            }
                            else if (fluReg.CodicePru != "" && !string.IsNullOrEmpty(fluReg.CodicePru))
                            {
                                #region Reg senza coordinate
                                string verso = "";
                                if (fluReg.Motivazione == "E" || fluReg.Motivazione == "U")
                                {
                                    verso = fluReg.Motivazione;
                                    fluReg.Motivazione = "";
                                }
                                else
                                {
                                    verso = fluReg.Verso;
                                }
                                if (fluReg.Motivazione == "none")
                                {
                                    fluReg.Motivazione = "";
                                }
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",
                                    fluReg.CodiceFru,
                                    fluReg.CodicePru,
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    fluReg.Data.Second.ToString("00"),
                                    verso,
                                    fluReg.Motivazione != "" ? "[Motivazione]=" + fluReg.Motivazione : null
                                );
                                #endregion
                            }
                            else if (string.IsNullOrEmpty(fluReg.CodicePru) && fluReg.Latitudine == 0 && fluReg.Longitudine == 0)
                            {
                                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",
                                    fluReg.CodiceFru,
                                    "WINIT00001",
                                    fluReg.Data.Year,
                                    fluReg.Data.Month.ToString("00"),
                                    fluReg.Data.Day.ToString("00"),
                                    fluReg.Data.Hour.ToString("00"),
                                    fluReg.Data.Minute.ToString("00"),
                                    fluReg.Data.Second.ToString("00"),
                                    fluReg.Verso,
                                    fluReg.Motivazione != "" ? "[Motivazione]=" + fluReg.Motivazione : null
                                );
                            }
                            regsToWrite.Add(regRow);
                            var noteLine = "";
                            if (fluReg.CodicePru != "" && !string.IsNullOrEmpty(fluReg.CodicePru))
                            {
                                noteLine = FlutterAppStringFormatter.CreateNoteLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            }
                            else
                            {
                                noteLine = FlutterAppStringFormatter.CreateNoteGpsLines(fluReg.CodiceFru, "", fluReg.Data, fluReg.Note);
                            }
                            if (noteLine != "")
                            {
                                regsToWrite.Add(noteLine);
                            }

                            String attivitaFinale = "";
                            if (fluReg.Attivita.Contains(','))
                            {
                                String[] att = fluReg.Attivita.Split(',');
                                attivitaFinale = att[1];
                            }
                            else
                            {
                                attivitaFinale = fluReg.Attivita;
                            }
                            if (attivitaFinale == "none")
                            {
                                attivitaFinale = "";
                            }
                            if (fluReg.Verso == "U" && attivitaFinale == "") 
                            {
                                Fru fru_id = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru == fluReg.CodicePru);

                                // inizializzazione della variabile di appoggio dell'associazione cant_fru da impostare
                                Fru_Cant currentFruCant = null;

                                if (fru_id != default)
                                {
                                    var fru_Cants = RepoManager.Fru_CantRepo.GetAllQueryable(fr => fr.Fru_Id == fru_id.Fru_Id).ToList();
                                    if (fru_Cants.Count() > 0)
                                    {
                                        DateTime oggi = new DateTime(fluReg.Data.Year, fluReg.Data.Month, fluReg.Data.Day, 0, 0, 0);
                                        if (fru_Cants.Count() > 1)
                                        {
                                            //se ho più associazioni devo controllare quella sia quella valida, mi creo le variabili di confronto di oggi e l'ultima data
                                            DateTime lastDate = default(DateTime);
                                            Fru_Cant lastFru = default(Fru_Cant);
                                            foreach (Fru_Cant assoc in fru_Cants)
                                            {
                                                if (oggi >= assoc.Abilitazione_Data_Inizio_Fru_Can) 
                                                {
                                                    //se la data di oggi è maggiore della data di associazione controllo se l'ultima data è default
                                                    if (lastDate == default(DateTime))
                                                    {
                                                        if (assoc == fru_Cants.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault())
                                                        {
                                                            //se è l'ultima della lista associo l'elemento corrente alla variabile finale
                                                            currentFruCant = assoc;
                                                        }
                                                        else 
                                                        {
                                                            //se è default vuol dire che è la prima associazione, associo all'ultima data la prima data d'associazione
                                                            lastDate = assoc.Abilitazione_Data_Inizio_Fru_Can;
                                                            lastFru = assoc;
                                                        }       
                                                    }
                                                    else 
                                                    {
                                                        //se non è default vuol dire che la data di oggi è più grande dell'associazione precedente
                                                        if (oggi >= assoc.Abilitazione_Data_Inizio_Fru_Can)
                                                        {
                                                            //se la data di oggi è maggiore a quella d'associazione controllo se è l'ultimo della lista
                                                            if (assoc == fru_Cants.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault())
                                                            {
                                                                //se è l'ultima della lista associo l'elemento corrente alla variabile finale
                                                                currentFruCant = assoc;
                                                            }
                                                            else 
                                                            {
                                                                //se non è l'ultimo elemento aggiorno le variabili temporanee
                                                                lastDate = assoc.Abilitazione_Data_Inizio_Fru_Can;
                                                                lastFru = assoc;
                                                            }   
                                                        }
                                                        else 
                                                        {
                                                            //se non è maggiore della data dell'associazione attuale metto nella associazione quella preedente
                                                            currentFruCant = lastFru;
                                                        }
                                                    }
                                                }
                                            }
                                            if (currentFruCant != null) 
                                            {
                                                //currentFruCant = fru_Cants.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault();
                                                Cant cantiere = RepoManager.CantRepo.FirstOrDefault(can => can.Cant_Id == currentFruCant.Cant_Id);
                                                List<Cant_Note> cantNote = RepoManager.Cant_NoteRepo.GetAllQueryable(cn => cn.Cant_Id == cantiere.Cant_Id).OrderBy(cn => cn.Data_Nota_Can_Note).ToList();
                                                if (cantNote.Count > 0)
                                                {
                                                    Cant_Note cnt = getLastAtt(cantNote, oggi);
                                                    if (cnt != null) 
                                                    {
                                                        Cant att = RepoManager.CantRepo.FirstOrDefault(can => can.Descrizione_Can == cnt.Nota_Can_Note && !can.DisAbilitazione_Can);
                                                        List<Fru_Cant> fru_Atts = RepoManager.Fru_CantRepo.GetAllQueryable(fr => fr.Cant_Id == att.Cant_Id).OrderBy(fr => fr.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                        attivitaFinale = fru_Atts.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault().Codice_Fru;
                                                    }
                                                }
                                            }
                                            
                                        }
                                        else 
                                        {
                                            currentFruCant = fru_Cants.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault();
                                            if (oggi >= currentFruCant.Abilitazione_Data_Inizio_Fru_Can) 
                                            {
                                                Cant cantiere = RepoManager.CantRepo.FirstOrDefault(can => can.Cant_Id == currentFruCant.Cant_Id);
                                                List<Cant_Note> cantNote = RepoManager.Cant_NoteRepo.GetAllQueryable(cn => cn.Cant_Id == cantiere.Cant_Id).OrderBy(cn => cn.Data_Nota_Can_Note).ToList();
                                                if (cantNote.Count > 0)
                                                {
                                                    Cant_Note cnt = getLastAtt(cantNote,oggi);
                                                    if (cnt != null) 
                                                    {
                                                        Cant att = RepoManager.CantRepo.FirstOrDefault(can => can.Descrizione_Can == cnt.Nota_Can_Note);
                                                        List<Fru_Cant> fru_Atts = RepoManager.Fru_CantRepo.GetAllQueryable(fr => fr.Cant_Id == att.Cant_Id).OrderBy(fr => fr.Abilitazione_Data_Inizio_Fru_Can).ToList();
                                                        attivitaFinale = fru_Atts.OrderByDescending(fr => fr.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault().Codice_Fru;
                                                    }
                                                }
                                            }
                                        }  
                                    }
                                }
                            }
                            var firstLine = FlutterAppStringFormatter.CreateFirstActivityLines(fluReg.CodiceFru, "", fluReg.Data, attivitaFinale);
                            if (firstLine != "")
                            {
                                regsToWrite.Add(firstLine);
                            }
                            var activityLine = FlutterAppStringFormatter.CreateActivityLines(fluReg.CodiceFru, "", fluReg.Data, attivitaFinale);
                            if (activityLine != "")
                            {
                                regsToWrite.Add(activityLine);
                            }
                            var pruCodeAtivity = FlutterAppStringFormatter.CreatePruCodeActivityLines(fluReg.CodiceFru, attivitaFinale, fluReg.Data, fluReg.CodiceFru);
                            if (pruCodeAtivity != null)
                            {
                                regsToWrite.AddRange(pruCodeAtivity);
                            }
                            #endregion
                        }

                        last = fluReg;
                        if (fluReg.Motivazione != "Pausa")
                        {
                            temp = fluReg;
                        }
                    }
                }
            }

            File.WriteAllLines(fileNamePatter, regsToWrite.ToArray());
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
    }
}
