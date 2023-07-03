using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.Repository;
using Newtonsoft.Json.Linq;
using log4net;
using Business.BusinessExtension;
using System.Reflection;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Windows;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Windows.Forms;
using Business.XmlExportsData.Perfetto;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Exports.ExportTxtCustom
{

    /**
    *  Export specifico verso software paghe Seldati del cliente SCS in formato .txt
    *  Export per periodo mensile
    *  Per ogni collaboratore, genera tanti record quante sono le causali presenti per il dato giorno.
    *  Ogni record avrà i seguente tracciato:
    *   - Codice tracciato  -> FISSO con valore 'P' (Presenze)
    *   - Anno              -> anno di riferimento
    *   - Mese              -> mese di riferimento
    *   - Filler1           -> campo riempitivo di 1 carattere ' ' (' ')
    *   - Codice azienda    -> codice identificativo dell'azienda nel programma Seldati (4311100 per SCS)
    *   - Codice dipendente -> codice identificativo del dipendente nel programma Seldati
    *   - Filler2           -> campo riempitivo di 2 caratteri '9' ('99')
    *   - Filler1           -> campo riempitivo di 1 carattere ' ' (' ')
    *   - Codice causale    -> codice identificativo della causale (vedere tabella causali)
    *   - Giorno            -> giorno di riferimento
    *   - Numero Ore        -> ore relative alla motivazione in centesimi in formato 000000000 (es: 8 ore e 30 minuti -> 000000850) 
    *   - Filler3           -> campo riempitivo di 22 caratteri ' ' ('                      ')
    *   - Tipo evento       -> Mal/Mat/Inf: “C”=Continuazione “R”=Ricaduta Cigo/Cigs: “P”=Posticipata 
    *   - Filler4           -> campo riempitivo di 2 caratteri ' ' ('  ')
    *   - Codice Cantiere   -> Codice cantiere in formato 000 (opzionale)
    *   - Filler5           -> campo riempitivo di 1 carattere 'E' ('E')
    **/

    /*
     * Si ricorda che l'export prevede i minuti in centesimi invece che in sessantesimi
     */

    class ExportBlueLine : TxtToolBox
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportBlueLine));

        #region Private Properties

        private const string _ordinaryHoursLabel = "ORD";
        private const string _extraOrdinaryHoursLabel = "STR";
        private const string _rpaHoursLabel = "RPA";
        private const string _trasfHoursLabel = "TRA";

        private const string _codiceTracciato = "P";
        private const string _codiceAzienda = "4311100";

        private string _anno;
        private string _mese;

        private const string _tipoEvento = " ";

        private const string _filler1 = " ";
        private const string _filler2 = "99 ";
        private static readonly string _filler3 = new string(' ', 22);
        private static readonly string _filler4 = new string(' ', 2);
        private const string _filler5 = "E";
        private static readonly string _filler6 = new string(' ', 21);
        private static readonly string _filler7 = new string(' ', 4);
        private static readonly string _fillergiorno = new string(' ', 52);
        private static readonly string _fillerfestivo = new string('+', 8);
        private static readonly string _fillerpre = new string('=', 8);
        private static readonly string _filler46 = new string(' ', 44);

        private static readonly string codiceAzienda = "RL10824";


        #endregion

        #region Constructor



        #endregion

        #region Public Methods

        public override void LaunchExport()
        {
            String[] dati = new string[2100];

            _anno = ExportDate.ToString("yyyy");
            _mese = ExportDate.Month.ToString("00");

            DateTime maxBound = ExportDate.AddMonths(1);

            string formattedDate = ExportDate.ToString("yyyy/MM");

            var allowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id)/* && (c.Matricola_Col != null && c.Matricola_Col != "")).Select(c => new { c.Col_Id, c.Codice_Collaboratore, c.Matricola_Col, c.Tab_Orari_Tipo_Id, c.Data_Disponibilita_Inizio_Col, c.Data_Disponibilita_Fine_Col }*/).ToList();

            List<string> notAllowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id) && (c.Matricola_Col == null || c.Matricola_Col == "")).Select(c => c.Codice_Collaboratore).ToList();

            notAllowedColls.ForEach(codiceCol =>
            {
                string error = String.Format("Il collaboratore con codice {0} non è stato esportato a causa dell'assenza del numero di matricola", codiceCol);
                _log.InfoFormat(error);
                Errors.Add(new { Messaggio = error });
            });


            foreach (var col in allowedColls)
            {
                string codice = String.Concat(col.Codice_Collaboratore.Where(c => !Char.IsWhiteSpace(c)));
                codice = CommonService.AggiungiZeriASinistra(codice, 6);
                int i = 1;
                string giornata = string.Format("{0}{1}{2}{3}",
                                   _anno,                   //0
                                   _mese,                   //1
                                   codiceAzienda,           //2
                                   codice        //3
                                   );

                #region Recupero orario

                if (col.Tab_Orari_Tipo_Id != null)
                {
                    var orario = RepoManager.Tab_OrariRepo.First(c => c.Tab_Orari_Tipo_Id == col.Tab_Orari_Tipo_Id);

                    int freeTimesheetId;
                    bool isFromFreeTimesheet;

                    Dictionary<int, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>>> planMinutes = RepoManager.Tab_OrariRepo.GetPlanMinutes(col.Col_Id,
                                                                   ExportDate, maxBound, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col,
                                                                    out isFromFreeTimesheet, out freeTimesheetId, false);

                    TimesheetModuleItem colPlan = TimesheetModuleItem.GenerateNewPlanTimesheet(false, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, ExportDate, planMinutes.First().Key);


                    #endregion

                    ILookup<DateTime?, Reg_V> dayDictionary = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(r => r.Col_Id == col.Col_Id && r.Cant_Id != null && r.Data_Reg_AAAA_MM == formattedDate && (r.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual) && r.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).OrderBy(c => c.Data_Reg).ToLookup(c => c.Data_Reg);

                    int giorno = 0;
                    int totaleGiorni = 31;
                    if (_mese == "11" || _mese == "04" || _mese == "06" || _mese == "09")
                    {
                        totaleGiorni = 30;
                    }
                    else if (_mese == "02")
                    {
                        totaleGiorni = 28;
                    }
                    bool festivita = false;
                    bool pre = false;

                    for (int k = 1; k <= totaleGiorni; k++) {
                        bool lavorato = false;
                        ILookup<DateTime?, Reg_V> element = RepoManager.Reg_VRepo.DbSet.AsNoTracking().Where(r => r.Col_Id == col.Col_Id && r.Cant_Id != null && r.Data_Reg_AAAA_MM == formattedDate && (r.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual) && r.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && r.Data_Ora_Fig_E.Value.Year == ExportDate.Year && r.Data_Ora_Fig_E.Value.Month == ExportDate.Month && r.Data_Ora_Fig_E.Value.Day == k).OrderBy(c => c.Data_Reg).ToLookup(c => c.Data_Reg);
                        foreach (IGrouping<DateTime?, Reg_V> day in element)
                        {
                            //if (giorno + 1 < day.Key.Value.Day)
                            //{
                            //    int diff = (day.Key.Value.Day - giorno) - 1;
                            //    for (int j = 0; j < diff; j++)
                            //    {
                            //        if (festivita)
                            //        {
                            //            giornata += (string.Format("{0}",
                            //                        _filler46    //0
                            //                        ));
                            //            festivita = false;
                            //        }
                            //        else if (pre)
                            //        {
                            //            giornata += (string.Format("{0}",
                            //                        _filler46    //0
                            //                        ));
                            //            pre = false;
                            //        }
                            //        else
                            //        {
                            //            giornata += (string.Format("{0}",
                            //                        _fillergiorno    //0
                            //                        ));
                            //        }
                            //    }
                            //}
                            List<Tab_Festivi> festa2 = RepoManager.Tab_FestiviRepo.GetAllQueryable(f => f.Giorno_Tab_Festivi.Day == k && f.Giorno_Tab_Festivi.Month == ExportDate.Month && f.Giorno_Tab_Festivi.Year == ExportDate.Year).ToList();
                            if (festa2.Count() > 0)
                            {
                                festivita = true;
                            }
                            List<Tab_Festivi> preFestivo2 = RepoManager.Tab_FestiviRepo.GetAllQueryable(f => f.Giorno_Tab_Festivi.Day == k + 1 && f.Giorno_Tab_Festivi.Month == ExportDate.Month && f.Giorno_Tab_Festivi.Year == ExportDate.Year).ToList();
                            if (preFestivo2.Count() > 0)
                            {
                                pre = true;
                            }
                            giorno = day.Key.Value.Day;

                            int maxDurationFromPlan = (int)colPlan.GetDayMinutes(day.Key.Value.Day);

                            //variabile con dentro la somma giornaliera senza motivazioni

                            int totalDaySum = (int)day.Where(reg => reg.Motivazione_Reg_Id == null && (reg.Registrazione_Tipo_Reg == 0 || reg.Registrazione_Tipo_Reg == 8)).Sum(reg => reg.Durata_Fig);  //Per il calcolo della percentuale giornaliera contano solo le ore lavorate

                            //recupero tutte le timbrature con motivazione e vado a compilare una lista con tutte le motivazioni della giornata

                            ILookup<string, Reg_V> regsByCant = day.OrderBy(c => c.Cant_Id).ThenBy(c => c.Motivazione_Reg_Cod).ToLookup(c => c.Motivazione_Reg_Cod);

                            IDictionary<string, int> daysDictionary = new Dictionary<string, int>();

                            foreach (var cantRegs in regsByCant)
                            {
                                double percIncidenzaGiornaliera = 0.0;

                                if (totalDaySum != 0)
                                {
                                    percIncidenzaGiornaliera = 100.0;
                                }

                                maxDurationFromPlan = RoundSixtyToThirty((Convert.ToInt32((maxDurationFromPlan * (double)percIncidenzaGiornaliera) / 100)));

                                IDictionary<string, int> cantDictionary = cantRegs.GroupBy(c => c.Motivazione_Reg_Cod).Select(c => new { Key = c.Key ?? "ORD", Sum = c.Sum(d => d.Durata_Fig ?? 0) }).ToDictionary(c => c.Key, d => (int)d.Sum);

                                

                                List<Tab_Festivi> festa = RepoManager.Tab_FestiviRepo.GetAllQueryable(f => f.Giorno_Tab_Festivi.Day == k && f.Giorno_Tab_Festivi.Month == ExportDate.Month && f.Giorno_Tab_Festivi.Year == ExportDate.Year).ToList();
                                if (festa.Count() > 0)
                                {
                                    festivita = true;
                                }
                                List<Tab_Festivi> preFestivo = RepoManager.Tab_FestiviRepo.GetAllQueryable(f => f.Giorno_Tab_Festivi.Day == k + 1 && f.Giorno_Tab_Festivi.Month == ExportDate.Month && f.Giorno_Tab_Festivi.Year == ExportDate.Year).ToList();
                                if (preFestivo.Count() > 0)
                                {
                                    pre = true;
                                }

                                if (!festivita)
                                {
                                    foreach (var rec in cantDictionary)
                                    {

                                        if (!daysDictionary.ContainsKey(rec.Key))
                                            daysDictionary[rec.Key] = 0;

                                        if (rec.Key == "ORD") //Trattamento ore ordinarie
                                        {
                                            int nonStr = (rec.Value > maxDurationFromPlan) ? maxDurationFromPlan : rec.Value;
                                            daysDictionary["ORD"] += nonStr;

                                            if (maxDurationFromPlan == 0)
                                            {
                                                daysDictionary["STR"] = 0;
                                                daysDictionary["STR"] = rec.Value;
                                            }
                                            else
                                            {
                                                daysDictionary["STR"] = 0;
                                                daysDictionary["STR"] += (rec.Value % daysDictionary["ORD"]);
                                            }


                                        }
                                        else if (rec.Key == _rpaHoursLabel) //Riposi compensativi accantonati
                                        {
                                            int rpaHours = rec.Value;

                                            if (daysDictionary.ContainsKey("STR") && daysDictionary["STR"] != 0)
                                            {
                                                if (rpaHours < 0)
                                                {
                                                    daysDictionary["STR"] += rpaHours;

                                                    if (daysDictionary["STR"] < 0)
                                                    {
                                                        daysDictionary["ORD"] += Math.Abs(daysDictionary["STR"]);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                daysDictionary["ORD"] += rpaHours;

                                            }

                                            daysDictionary[_rpaHoursLabel] = Math.Abs(rec.Value);
                                        }
                                        else //Altre motivazioni
                                        {
                                            daysDictionary[rec.Key] = 0;
                                            daysDictionary[rec.Key] += rec.Value;
                                        }
                                    }
                                 // int m = 0;
                                 // maxDurationFromPlan = colPlan.GetDayMinutes(day.Key.Value.Day);
                                 // DateTime oggi = new DateTime(ExportDate.Year, ExportDate.Month, k);
                                 // foreach (var motKey in daysDictionary.ToList())
                                 // {
                                 //     if (motKey.Key == "ORD")
                                 //     {
                                 //         if (motKey.Value > 0)
                                 //         {
                                 //             double ore = motKey.Value / 60;
                                 //             double minuti = motKey.Value % 60;
                                 //             int minut = Convert.ToInt32(Math.Round(Convert.ToDouble(minuti) * 100 / 60, 0));
                                 //             double piano = maxDurationFromPlan / 60;
                                 //             string prova = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                 //             string min = CommonService.AggiungiZeriASinistra(minut.ToString(), 2);
                                 //             if (ore < 10 && piano > 10)
                                 //             {
                                 //                 string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2);
                                 //                 giornata += (string.Format("{0}{1}{2}",
                                 //                 CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(maxDurationFromPlan / 30.0) * 30).ToString(), 2),     //0
                                 //                 oreLav, //1
                                 //                 min     //2
                                 //                 ));
                                 //             }
                                 //             else if (ore > 10 && piano < 10)
                                 //             {
                                 //                 if (piano == 0)
                                 //                 {
                                 //                     string orePiano = "0000";
                                 //                     giornata += (string.Format("{0}{1}{2}",
                                 //                     orePiano,     //0
                                 //                     ((motKey.Value / 30.0) * 30).ToString(),   //1
                                 //                     min             //2
                                 //                     ));
                                 //                 }
                                 //                 else
                                 //                 {
                                 //                     string orePiano = CommonService.AggiungiZeriASinistra(piano.ToString(), 2);
                                 //                     orePiano = CommonService.AggiungiZeriADestra(orePiano, 4);
                                 //                     giornata += (string.Format("{0}{1}{2}",
                                 //                     orePiano,     //0
                                 //                     ((motKey.Value / 30.0) * 30).ToString(),   //1
                                 //                     min             //2
                                 //                     ));
                                 //                 }
                                 //             }
                                 //             else
                                 //             {
                                 //                 string orePiano = CommonService.AggiungiZeriASinistra(piano.ToString(), 2);
                                 //                 orePiano = CommonService.AggiungiZeriADestra(orePiano, 4);
                                 //                 string oreLav = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                 //                 giornata += (string.Format("{0}{1}{2}",
                                 //                 orePiano,     //0
                                 //                 oreLav,     //1
                                 //                 min         //2
                                 //                 ));
                                 //
                                 //             }
                                 //         } else if (motKey.Value == 0 && (maxDurationFromPlan / 60) == 0 && (pre || oggi.DayOfWeek == DayOfWeek.Saturday)) {
                                 //             giornata += "========";
                                 //         }
                                 //         else if (motKey.Value == 0 && (maxDurationFromPlan / 60) == 0)
                                 //         {
                                 //             giornata += "00000000";
                                 //         }
                                 //         else
                                 //         {
                                 //             giornata += (string.Format("{0}{1}",
                                 //                    _filler7,     //0
                                 //                    _filler7      //1
                                 //                    ));
                                 //         }
                                 //         m++;
                                 //     }
                                 //     else if (motKey.Key == "STR")
                                 //     {
                                 //         double ore = motKey.Value / 60;
                                 //         double minuti = motKey.Value % 60;
                                 //         int minut = Convert.ToInt32(Math.Round(Convert.ToDouble(minuti) * 100 / 60, 0));
                                 //         string min = CommonService.AggiungiZeriASinistra(minut.ToString(), 2);
                                 //         if (ore > 0)
                                 //         {
                                 //             if (ore < 10)
                                 //             {
                                 //                 string oreLav = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                 //                 giornata += (string.Format("{0}{1}",
                                 //                 CommonService.AggiungiZeriADestra(oreLav, 2),     //0
                                 //                 min
                                 //                 ));
                                 //             }
                                 //             else if (ore > 10)
                                 //             {
                                 //                 giornata += (string.Format("{0}{1}",
                                 //                 CommonService.AggiungiZeriADestra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2),     //1
                                 //                 min
                                 //                 ));
                                 //             }
                                 //         }
                                 //         else
                                 //         {
                                 //             giornata += (string.Format("{0}",
                                 //                             _filler7     //0
                                 //                             ));
                                 //         }
                                 //         m++;
                                 //     }
                                 //     else
                                 //     {
                                 //         string motivazione = motKey.Key;
                                 //         int riempimento = 4 - motivazione.Length;
                                 //         if (motivazione.Length < 4)
                                 //         {
                                 //             motivazione = CommonService.AggiungiSpaziiADestra(motivazione, 4);
                                 //         }
                                 //         if (m == 2)
                                 //         {
                                 //             string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2);
                                 //             giornata += (string.Format("{0}{1}",
                                 //                 motKey.Key,
                                 //             CommonService.AggiungiZeriADestra(oreLav, 4)     //0
                                 //             ));
                                 //         }
                                 //         else if (m == 1)
                                 //         {
                                 //             motivazione = CommonService.AggiungiSpaziASinistra(motivazione, 10 + riempimento);
                                 //             string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 4);
                                 //             giornata += (string.Format("{0}{1}",
                                 //                 motivazione,
                                 //             CommonService.AggiungiZeriADestra(oreLav, 3)     //0
                                 //             ));
                                 //         }
                                 //         else
                                 //         {
                                 //             motivazione = CommonService.AggiungiSpaziASinistra(motivazione, 14 + riempimento);
                                 //             string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 4);
                                 //             oreLav = CommonService.AggiungiZeriADestra(oreLav, 3);
                                 //             giornata += (string.Format("{0}{1}",
                                 //                 motivazione,
                                 //                 oreLav    //0
                                 //             ));
                                 //         }
                                 //     }
                                 // }
                                    
                                }
                            }
                            int m = 0;
                            maxDurationFromPlan = colPlan.GetDayMinutes(day.Key.Value.Day);
                            DateTime oggi = new DateTime(ExportDate.Year, ExportDate.Month, k);
                            foreach (var motKey in daysDictionary.OrderBy(d => d.Key).ToList())
                            {
                                if (motKey.Key == "ORD")
                                {
                                    if (motKey.Value > 0)
                                    {
                                        double ore = motKey.Value / 60;
                                        double minuti = motKey.Value % 60;
                                        int minut = Convert.ToInt32(Math.Round(Convert.ToDouble(minuti) * 100 / 60, 0));
                                        double piano = maxDurationFromPlan / 60;
                                        string prova = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                        string min = CommonService.AggiungiZeriASinistra(minut.ToString(), 2);
                                        if (ore < 10 && piano > 10)
                                        {
                                            string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2);
                                            giornata += (string.Format("{0}{1}{2}",
                                            CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(maxDurationFromPlan / 30.0) * 30).ToString(), 2),     //0
                                            oreLav, //1
                                            min     //2
                                            ));
                                        }
                                        else if (ore > 10 && piano < 10)
                                        {
                                            if (piano == 0)
                                            {
                                                string orePiano = "0000";
                                                giornata += (string.Format("{0}{1}{2}",
                                                orePiano,     //0
                                                ((motKey.Value / 30.0) * 30).ToString(),   //1
                                                min             //2
                                                ));
                                            }
                                            else
                                            {
                                                string orePiano = CommonService.AggiungiZeriASinistra(piano.ToString(), 2);
                                                orePiano = CommonService.AggiungiZeriADestra(orePiano, 4);
                                                giornata += (string.Format("{0}{1}{2}",
                                                orePiano,     //0
                                                ((motKey.Value / 30.0) * 30).ToString(),   //1
                                                min             //2
                                                ));
                                            }
                                        }
                                        else
                                        {
                                            string orePiano = CommonService.AggiungiZeriASinistra(piano.ToString(), 2);
                                            orePiano = CommonService.AggiungiZeriADestra(orePiano, 4);
                                            string oreLav = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                            giornata += (string.Format("{0}{1}{2}",
                                            orePiano,     //0
                                            oreLav,     //1
                                            min         //2
                                            ));

                                        }
                                    }
                                    else if (motKey.Value == 0 && (maxDurationFromPlan / 60) == 0 && (pre || oggi.DayOfWeek == DayOfWeek.Saturday))
                                    {
                                        giornata += "========";
                                    }
                                    else if (motKey.Value == 0 && (maxDurationFromPlan / 60) == 0)
                                    {
                                        giornata += "00000000";
                                    }
                                    else
                                    {
                                        giornata += (string.Format("{0}{1}",
                                               _filler7,     //0
                                               _filler7      //1
                                               ));
                                    }
                                    m++;
                                }
                                else if (motKey.Key == "STR")
                                {
                                    double ore = motKey.Value / 60;
                                    double minuti = motKey.Value % 60;
                                    int minut = Convert.ToInt32(Math.Round(Convert.ToDouble(minuti) * 100 / 60, 0));
                                    string min = CommonService.AggiungiZeriASinistra(minut.ToString(), 2);
                                    if (ore > 0)
                                    {
                                        if (ore < 10)
                                        {
                                            string oreLav = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                            giornata += (string.Format("{0}{1}",
                                            CommonService.AggiungiZeriADestra(oreLav, 2),     //0
                                            min
                                            ));
                                        }
                                        else if (ore > 10)
                                        {
                                            giornata += (string.Format("{0}{1}",
                                            CommonService.AggiungiZeriADestra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2),     //1
                                            min
                                            ));
                                        }
                                    }
                                    else
                                    {
                                        giornata += (string.Format("{0}",
                                                        _filler7     //0
                                                        ));
                                    }
                                    m++;
                                }
                                else
                                {
                                    string motivazione = motKey.Key;
                                    int riempimento = 4 - motivazione.Length;
                                    if (motivazione.Length < 4)
                                    {
                                        motivazione = CommonService.AggiungiSpaziiADestra(motivazione, 4);
                                    }
                                    if (m == 2)
                                    {
                                        string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 2);
                                        giornata += (string.Format("{0}{1}",
                                            motKey.Key,
                                        CommonService.AggiungiZeriADestra(oreLav, 4)     //0
                                        ));
                                    }
                                    else if (m == 1)
                                    {
                                        motivazione = CommonService.AggiungiSpaziASinistra(motivazione, 10 + riempimento);
                                        string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 4);
                                        giornata += (string.Format("{0}{1}",
                                            motivazione,
                                        CommonService.AggiungiZeriADestra(oreLav, 3)     //0
                                        ));
                                    }
                                    else
                                    {
                                        motivazione = CommonService.AggiungiSpaziASinistra(motivazione, 14 + riempimento);
                                        string oreLav = CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 4);
                                        oreLav = CommonService.AggiungiZeriADestra(oreLav, 3);
                                        giornata += (string.Format("{0}{1}",
                                            motivazione,
                                            oreLav    //0
                                        ));
                                    }
                                }
                            }
                            if ((giornata.Length - 19) / k < 52)
                            {
                                int riempimento = 52 - (giornata.Length - 19) / k;
                                giornata = CommonService.CompletaADestra(giornata, (52 * k) + 19);
                            }
                            lavorato = true;
                        }
                        //nel caso non abbia lavorato completo il file
                        if (!lavorato)
                        {
                            DateTime oggi = new DateTime(ExportDate.Year, ExportDate.Month, k);
                            if (festivita || oggi.DayOfWeek == DayOfWeek.Sunday)
                            {
                                giornata += (string.Format("{0}{1}",
                                                    _fillerfestivo, //0
                                                    _filler46       //1
                                                    ));
                                festivita = false;
                            }
                            else if (pre || oggi.DayOfWeek == DayOfWeek.Saturday)
                            {
                                giornata += (string.Format("{0}{1}",
                                                    _fillerpre,  //0
                                                    _filler46    //1
                                                    ));
                                pre = false;
                            }
                            else
                            {
                                int maxDurationFromPlan = (int)colPlan.GetDayMinutes(k);
                                maxDurationFromPlan = colPlan.GetDayMinutes(k);
                                double piano = maxDurationFromPlan / 60;
                                string orePiano = CommonService.AggiungiZeriASinistra(piano.ToString(), 2);
                                orePiano = CommonService.AggiungiZeriADestra(orePiano, 4);
                                if (maxDurationFromPlan > 0)
                                {
                                    giornata += (string.Format("{0}{1}{2}",
                                                    orePiano,
                                                    "0000",//0
                                                    _filler46
                                                    ));
                                }
                                else
                                {
                                    giornata += (string.Format("{0}",
                                                       _fillergiorno    //0
                                                       ));
                                }
                            }
                                
                            
                        }
                        else {
                            pre = false;
                            festivita = false;
                        }
                    }
                    TxtLines.Add(giornata);
                }
                else
                {
                    string error = String.Format("Il collaboratore con codice {0} non è stato esportato perchè sprovvisto di orario", col.Codice_Collaboratore);
                    _log.InfoFormat(error);
                    Errors.Add(new { Messaggio = error });
                }
            }
        }

        #endregion

        #region Private Methods

        private int ToCent(int minutes)
        {
            return Convert.ToInt32(((minutes / 60.0) * 100));
        }

        private int ToSixty(int minutes)
        {
            return Convert.ToInt32(((minutes / 100.0) * 60));
        }

        private int RoundSixtyToThirty(int minutes)
        {
            return Convert.ToInt32(Math.Round((double)minutes / 30) * 30);
        }

        private void InitMotivationRegistry(Dictionary<Cant, Dictionary<string, int>> register, Cant cant, string mot)
        {
            if (!register[cant].ContainsKey(mot))
                register[cant][mot] = 0;
        }

        #endregion
    }
}
