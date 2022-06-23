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

    class ExportTxtSCS : TxtToolBox
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTxtSCS));

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


        #endregion

        #region Constructor



        #endregion

        #region Public Methods

        public override void LaunchExport()
        {

            _anno = ExportDate.ToString("yy");
            _mese = ExportDate.Month.ToString("00");

            DateTime maxBound = ExportDate.AddMonths(1);

            string formattedDate = ExportDate.ToString("yyyy/MM");

            var allowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id) && (c.Matricola_Col != null && c.Matricola_Col != "")).Select(c => new { c.Col_Id, c.Codice_Collaboratore, c.Matricola_Col, c.Tab_Orari_Tipo_Id, c.Data_Disponibilita_Inizio_Col, c.Data_Disponibilita_Fine_Col }).ToList();

            List<string> notAllowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id) && (c.Matricola_Col == null || c.Matricola_Col == "")).Select(c => c.Codice_Collaboratore).ToList();

            notAllowedColls.ForEach(codiceCol =>
            {
                string error = String.Format("Il collaboratore con codice {0} non è stato esportato a causa dell'assenza del numero di matricola", codiceCol);
                _log.InfoFormat(error);
                Errors.Add(new { Messaggio = error });
            });


            foreach (var col in allowedColls)
            {
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

                    foreach (IGrouping<DateTime?, Reg_V> day in dayDictionary)
                    {
                        Dictionary<Cant, Dictionary<string, int>> dayRegister = new Dictionary<Cant, Dictionary<string, int>>();

                        string currentDay = day.Key.Value.Day.ToString("00");

                        int maxDurationFromPlan = (int)colPlan.GetDayMinutes(day.Key.Value.Day);
                        int totalDaySum = (int)day.Where(reg => reg.Motivazione_Reg_Id == null && (reg.Registrazione_Tipo_Reg == 0 || reg.Registrazione_Tipo_Reg == 8)).Sum(reg => reg.Durata_Fig);  //Per il calcolo della percentuale giornaliera contano solo le ore lavorate

                        ILookup<int?, Reg_V> regsByCant = day.OrderBy(c => c.Cant_Id).ThenBy(c => c.Motivazione_Reg_Cod).ToLookup(c => c.Cant_Id);

                        foreach (var cantRegs in regsByCant)
                        {
                            Cant regsCant = RepoManager.CantRepo.DbSet.Find(cantRegs.Key);

                            string currentCantCode = regsCant.Codice_Gestionale_Can;

                            if (currentCantCode == null)
                            {
                                string error = String.Format("Il cantiere con codice {0} non è stato esportato a causa dell'assenza del numero di matricola", regsCant.Codice_Cantiere);

                                if (!Errors.Contains(error))
                                {
                                    _log.InfoFormat(error);
                                    Errors.Add(new { Messaggio = error });
                                }
                                continue;
                            }

                            double sommaDelCantiere = (double)cantRegs.Where(reg => reg.Motivazione_Reg_Id == null && ( reg.Registrazione_Tipo_Reg == 0 || reg.Registrazione_Tipo_Reg == 8)).Sum(c => c.Durata_Fig); //Per il calcolo della percentuale giornaliera contano solo le ore lavorate

                            double percIncidenzaGiornaliera = 0.0;

                            if (totalDaySum == 0)
                            {
                                percIncidenzaGiornaliera = 100.0;
                            }
                            else
                            {
                                percIncidenzaGiornaliera = Math.Ceiling((sommaDelCantiere / (double)totalDaySum) * 100);
                            }

                            maxDurationFromPlan = RoundSixtyToThirty((Convert.ToInt32((maxDurationFromPlan * (double)percIncidenzaGiornaliera) / 100)));

                            dayRegister[regsCant] = new Dictionary<string, int>();

                            IDictionary<string, int> cantDictionary = cantRegs.GroupBy(c => c.Motivazione_Reg_Cod).Select(c => new { Key = c.Key ?? "ORD", Sum = c.Sum(d => d.Durata_Fig ?? 0) }).ToDictionary(c => c.Key, d => (int)d.Sum);

                            IDictionary<string, int> daysDictionary = new Dictionary<string, int>();


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
                            
                            maxDurationFromPlan = colPlan.GetDayMinutes(day.Key.Value.Day);
                            if (currentCantCode.Length == 4)
                            {
                                foreach (var motKey in daysDictionary.Where(c => c.Value != 0).ToList())
                                {
                                    TxtLines.Add(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                                    _codiceTracciato,        //0
                                    _anno,                   //1
                                    _mese,                   //2
                                    _filler1,                //3
                                    _codiceAzienda,          //4
                                    col.Matricola_Col,        //5
                                    _filler2,                //6
                                    motKey.Key,                 //7
                                    currentDay,                  //8
                                    CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 9),              //9
                                    _filler6,                //10
                                    _tipoEvento,             //11
                                    _filler4,                //12
                                    currentCantCode,         //13
                                    _filler5                 //14
                                    ));
                                }
                            }
                            else {
                                foreach (var motKey in daysDictionary.Where(c => c.Value != 0).ToList())
                                {
                                    TxtLines.Add(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                                    _codiceTracciato,        //0
                                    _anno,                   //1
                                    _mese,                   //2
                                    _filler1,                //3
                                    _codiceAzienda,          //4
                                    col.Matricola_Col,        //5
                                    _filler2,                //6
                                    motKey.Key,                 //7
                                    currentDay,                  //8
                                    CommonService.AggiungiZeriASinistra(ToCent((int)Math.Round(motKey.Value / 30.0) * 30).ToString(), 9),              //9
                                    _filler3,                //10
                                    _tipoEvento,             //11
                                    _filler4,                //12
                                    currentCantCode,         //13
                                    _filler5                 //14
                                    ));
                                }
                            }
                            

                        }
                    }
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
