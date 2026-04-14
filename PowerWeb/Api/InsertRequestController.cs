using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading;
using System.Web;
using System.Web.Http;
using Business;
using Business.DataClasses.SupportClasses;
using Business.Profile;
using Business.Repository;
using Common;
using Domain;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Business.DataClasses.WebApiDataClasses;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using DevExpress.Web.ASPxTitleIndex.Internal;
using System.Web.Helpers;

namespace PowerWeb.Api
{

    /// <summary>
    /// Classe utilizzata per esporre tramite web api la procedura di importazione file con le registrazioni
    /// </summary>
    public class InsertRequestController : GenericApi
    {

        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        private static readonly ILog _log = LogManager.GetLogger(typeof(Domain.Reg));
        /// <summary>
        /// Esegue l'operazione di import oggetto della API.
        /// </summary>
        protected override void ExecuteOperation()
        {
            _log.Info("Inizio ad inserire la richiesta di ferie o permesso");
            bool result = true;
            ReturnValues values = new ReturnValues();
            if (Start != new TimeSpan(0, 1, 0))
            {
                //Url tipo: http://localhost:55513//api/InsertRequest//?from=2025-02-27T00:00:00.00000000&start=08%3A00%3A00&end=12%3A00%3A00&matricola=49972&justification=RIchiesta Permesso
                //se sono qui vuol dire che è una richiesta oraria
                List<Reg> regsToAdd = new List<Reg>();
                List<Reg> regsToElaborate = new List<Reg>();
                Pru pru = null;
                Fru fru = null;
                Pru_Col pruCol = null;
                Fru_Cant fruCant = null;
                Col col = null;
                Cant can = null;
                Tab_Decod motivazione = null;
                List<Reg> checkRegs = new List<Reg>();
                try 
                {
                    //in base ai dati ricevuti tramite parmetro recupero le matricole e i relativi cantieri e collaboratori associati
                    pru = RepoManager.PruRepo.Single(p => p.Codice_Pru == "     " + ColId);
                    fru = RepoManager.FruRepo.Single(f => f.Codice_Fru == "MOTIV00001");
                    var codicePru = "     " + ColId.ToString(); // o la proprietà giusta

                    var pruId = RepoManager.Pru_ColRepo
                        .GetAllQueryable(p => p.Pru_Id == pru.Pru_Id)
                        .OrderByDescending(p => p.Abilitazione_Data_Inizio_Pru_Col)
                        .Select(p => p.Pru_Id)
                        .FirstOrDefault();

                    pruCol = RepoManager.Pru_ColRepo.GetAllQueryable(pr => pr.Pru_Id == pruId).OrderByDescending(pr => pr.Abilitazione_Data_Inizio_Pru_Col).FirstOrDefault();

                    var fruId = RepoManager.Fru_CantRepo
                        .GetAllQueryable(p => p.Fru_Id == fru.Fru_Id)
                        .OrderByDescending(p => p.Abilitazione_Data_Inizio_Fru_Can)
                        .Select(p => p.Fru_Id)
                        .FirstOrDefault();

                    fruCant = RepoManager.Fru_CantRepo.GetAllQueryable(fc => fc.Fru_Id == fruId).OrderByDescending(fc => fc.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault();
                    col = RepoManager.ColRepo.Single(c => c.Col_Id == pruCol.Col_Id);
                    can = RepoManager.CantRepo.Single(c => c.Cant_Id == fruCant.Cant_Id);
                    motivazione = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == Justification && td.Nome_Tab == "MOTIVAZIONI");
                } catch (Exception e) {
                    //nel caso in cui ci sia un eccezione lo notifico nella response e non faccio inserire i dati
                    values.Status = false;
                    values.Message = "Dati forniti come parametri non corretti";
                    JsonData = JsonConvert.SerializeObject(values);
                    result = false;
                    _log.ErrorFormat("Errore nella convalida dei dati forniti {0}", e.InnerException);
                }
                
                DateTime dataE = new DateTime(From.Year,From.Month,From.Day,Start.Hours,Start.Minutes,Start.Seconds);
                var dataPrimoGiorno = From.ToString().Split(' ');
                string noteReg = "" + dataPrimoGiorno[0];
                if (result) {
                    Reg regE = new Reg
                    {
                        Fru_Id = fru.Fru_Id,
                        Pru_Id = pru.Pru_Id,
                        Cant_Id = can.Cant_Id,
                        Col_Id = col.Col_Id,
                        Registrazione_Data_Ora_Fis_Reg = dataE,
                        Registrazione_Data_Ora_Fig_Reg = dataE,
                        Registrazione_Data_Ora_Orig_Reg = dataE,
                        Registrazione_Stato_Reg = (int)RegStateEnum.Ass,
                        Data_Registrazione_Reg = DateTime.UtcNow,
                        DataOraUltimaModifica_Reg = DateTime.UtcNow,
                        Motivazione_Reg_Id = motivazione.Tab_Decod_Id,
                        Note_Reg = noteReg
                    };
                    checkRegs = RepoManager.RegRepo.GetAllQueryable(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg == dataE).ToList();
                    if (checkRegs.Count() == 0)
                    {
                        RepoManager.RegRepo.Add(regE, true);
                        //regE = RepoManager.RegRepo.Single(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg == dataE);
                        DateTime dataU = new DateTime(From.Year, From.Month, From.Day, End.Hours, End.Minutes, End.Seconds);
                        Reg regU = new Reg
                        {
                            Fru_Id = fru.Fru_Id,
                            Pru_Id = pru.Pru_Id,
                            Cant_Id = can.Cant_Id,
                            Col_Id = col.Col_Id,
                            Registrazione_Data_Ora_Fis_Reg = dataU,
                            Registrazione_Data_Ora_Fig_Reg = dataU,
                            Registrazione_Data_Ora_Orig_Reg = dataU,
                            RiferimentoRRN_Reg = regE.Reg_Id,
                            Registrazione_Stato_Reg = (int)RegStateEnum.Ass,
                            Data_Registrazione_Reg = DateTime.UtcNow,
                            DataOraUltimaModifica_Reg = DateTime.UtcNow,
                            Motivazione_Reg_Id = motivazione.Tab_Decod_Id,
                            Note_Reg = noteReg
                        };
                        RepoManager.RegRepo.Add(regU, true);
                        values.Status = true;
                        values.Message = "Richiesta inserita correttamente";
                        _log.Info("Inizio ad inserire la richiesta di ferie o permesso");
                        InviaConferma(1, col, From, To);
                    }
                    else {
                        _log.Info("Rchiesta gia presente a sistema");
                        values.Status = false;
                        values.Message = "Richiesta già presente a sistema";
                        InviaConferma(3, col, From, To);
                    }
                    
                    JsonData = JsonConvert.SerializeObject(values);
                }
            }
            else 
            {
                //url tipo http://localhost:55513//api/InsertRequest//?from=2025-02-24T00:00:00.00000000&to=2025-02-27T00:00:00.00000000&matricola=50044&justification=RIchiesta Ferie
                //se sono qui è una richiesta di una giornata intera
                List<Reg> regsToAdd = new List<Reg>();
                List<Reg> regsToElaborate = new List<Reg>();
                Pru pru = null;
                Fru fru = null;
                Pru_Col pruCol = null;
                Fru_Cant fruCant = null;
                Col col = null;
                Cant can = null;
                Tab_Decod motivazione = null;
                List<Reg> checkRegs = new List<Reg>();
                bool inserted = true;
                try {
                    //in base ai dati ricevuti tramite parmetro recupero le matricole e i relativi cantieri e collaboratori associati
                    pru = RepoManager.PruRepo.Single(p => p.Codice_Pru == "     " + ColId);
                    fru = RepoManager.FruRepo.Single(f => f.Codice_Fru == "MOTIV00001");
                    var codicePru = "     " + ColId.ToString(); // o la proprietà giusta

                    var pruId = RepoManager.Pru_ColRepo
                        .GetAllQueryable(p => p.Pru_Id == pru.Pru_Id)
                        .OrderByDescending(p => p.Abilitazione_Data_Inizio_Pru_Col)
                        .Select(p => p.Pru_Id)
                        .FirstOrDefault();

                    pruCol = RepoManager.Pru_ColRepo.GetAllQueryable(pr => pr.Pru_Id == pruId).OrderByDescending(pr => pr.Abilitazione_Data_Inizio_Pru_Col).FirstOrDefault();

                    var fruId = RepoManager.Fru_CantRepo
                        .GetAllQueryable(p => p.Fru_Id == fru.Fru_Id)
                        .OrderByDescending(p => p.Abilitazione_Data_Inizio_Fru_Can)
                        .Select(p => p.Fru_Id)
                        .FirstOrDefault();

                    fruCant = RepoManager.Fru_CantRepo.GetAllQueryable(fc => fc.Fru_Id == fruId).OrderByDescending(fc => fc.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault();
                    col = RepoManager.ColRepo.Single(c => c.Col_Id == pruCol.Col_Id);
                    can = RepoManager.CantRepo.Single(c => c.Cant_Id == fruCant.Cant_Id);
                    motivazione = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == Justification && td.Nome_Tab == "MOTIVAZIONI");
                } catch (Exception e) {
                    //nel caso in cui ci sia un eccezione lo notifico nella response e non faccio inserire i dati
                    values.Status = false;
                    values.Message = "Dati forniti come parametri non corretti";
                    JsonData = JsonConvert.SerializeObject(values);
                    result = false;
                    _log.ErrorFormat("Errore nella convalida dei dati forniti {0}",e.InnerException);
                }
                if (result) {
                    DateTime from = From;
                    int regs = 1;
                    string noteReg = "";
                    while (from <= To)
                    {
                        if (from.DayOfWeek != DayOfWeek.Saturday && from.DayOfWeek != DayOfWeek.Sunday)
                        {
                            if (regs == 1)
                            {
                                var dataPrimoGiorno = From.ToString().Split(' ');
                                var dataReg = To.ToString().Split(' ');
                                noteReg = "" + dataPrimoGiorno[0] + "-" + dataReg[0];
                            }

                            DateTime dataE = new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute, from.Second);
                            Reg regE = new Reg
                            {
                                Fru_Id = fru.Fru_Id,
                                Pru_Id = pru.Pru_Id,
                                Cant_Id = can.Cant_Id,
                                Col_Id = col.Col_Id,
                                Registrazione_Data_Ora_Fis_Reg = dataE,
                                Registrazione_Data_Ora_Fig_Reg = dataE,
                                Registrazione_Data_Ora_Orig_Reg = dataE,
                                Data_Registrazione_Reg = DateTime.UtcNow,
                                DataOraUltimaModifica_Reg = DateTime.UtcNow,
                                Motivazione_Reg_Id = motivazione.Tab_Decod_Id,
                                Registrazione_Stato_Reg = (int)RegStateEnum.Ass,
                                Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration,
                                Rettifica_Durata = Convert.ToInt32(480),
                                Note_Reg = noteReg
                            };
                            checkRegs = RepoManager.RegRepo.GetAllQueryable(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg == dataE).ToList();
                            if (checkRegs.Count() == 0)
                            {
                                regsToAdd.Add(regE);
                            }
                            else {
                                inserted = false;
                            }
                            
                        }
                        from = from.AddDays(1);
                    }
                    if (inserted)
                    {
                        _log.Info("Richiesta inserita correttamente a sistema");
                        RepoManager.RegRepo.Add(regsToAdd, true);
                        values.Status = true;
                        values.Message = "Richiesta inserita correttamente";
                        InviaConferma(0, col, From, To);
                    }
                    else {
                        _log.Info("Rchiesta gia presente a sistema");
                        values.Status = false;
                        values.Message = "Richiesta già presente a sistema";
                        InviaConferma(2, col, From, To);
                    }                    
                    JsonData = JsonConvert.SerializeObject(values);
                }
               
            }
        }

        public string oreCentesimi(int minuti)
        {
            string result = "";
            TimeSpan totalDuration = TimeSpan.FromMinutes(minuti);

            return result = String.Format("{0}{1},{2}", (totalDuration < TimeSpan.Zero ? "-" : ""), Math.Abs((totalDuration.Days * 24) + totalDuration.Hours), FromMinutesToCent(Math.Abs(totalDuration.Minutes)));

        }

        protected string FromMinutesToCent(int minutes)
        {
            return ((minutes / 60.0) * 100).ToString("00");
        }

        public string InviaConferma(int esito, Col cols, DateTime from, DateTime to)
        {
            string titoloMail = "";
            string mailTo = RepoManager.ParamRepo.ParametersRow.CompanyEmail;

            //Prepara il body della mail caricando il css
            string mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">",
                   errorMessage = "";
            switch (esito)
            {
                case 0:
                    titoloMail = "PowerWeb - Richiesta di ferie inserita correttamente";
                    mailBody += "<p>La richiesta di ferie per il periodo " + from.ToString("dddd d MMMM yyyy") + "-" + to.ToString("dddd d MMMM yyyy") + " richieste dal collaboratore " + cols.CognomeNome_Col + " sono state inserite</p>";
                    break;
                case 1:
                    titoloMail = "PowerWeb - Richiesta di permesso inserita correttamente";
                    mailBody += "<p>la richiesta di permesso per il giorno " + from.ToString("dddd d MMMM yyyy") + " richiesto dal collaboratore " + cols.CognomeNome_Col + " è stato inserito </p>";
                    break;
                case 2:
                    titoloMail = "PowerWeb - Richiesta di ferie già presente";
                    mailBody += "<p>La richiesta di ferie per il periodo " + from.ToString("dddd d MMMM yyyy") + "-" + to.ToString("dddd d MMMM yyyy") + " richieste dal collaboratore " + cols.CognomeNome_Col + " sono già presenti a sistema</p>";
                    break;
                case 3:
                    titoloMail = "PowerWeb - Richiesta di permesso già presente";
                    mailBody += "<p>La richiesta di permesso per il giorno " + from.ToString("dddd d MMMM yyyy") + " richiesto dal collaboratore " + cols.CognomeNome_Col + " è già inserito a sistema </p>";
                    break;
                default:
                    break;
            }

            mailBody += "</div>";

            if (cols.Email_Col != "" && cols.Email_Col != null)
            {
                mailTo = mailTo + ";" + cols.Email_Col;
            }

            errorMessage = CommonService.sendMail(mailTo, titoloMail, mailBody, "newsletter@winit.it", titoloMail, new string[] { });

            DateTime today = DateTime.Today;

            return errorMessage;
        }
    }

}