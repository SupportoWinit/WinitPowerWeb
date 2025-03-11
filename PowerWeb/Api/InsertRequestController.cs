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
            if (Start != new TimeSpan(0, 1, 0))
            {
                //Url tipo: http://localhost:55513//api/InsertRequest//?from=2025-02-27T00:00:00.00000000&start=08%3A00%3A00&end=12%3A00%3A00&matricola=49972&justification=RIchiesta Permesso
                //se sono qui vuol dire che è una richiesta oraria
                List<Reg> regsToAdd = new List<Reg>();
                List<Reg> regsToElaborate = new List<Reg>();
                //in base ai dati ricevuti tramite parmetro recupero le matricole e i relativi cantieri e collaboratori associati
                Pru pru = RepoManager.PruRepo.Single(p => p.Codice_Pru == "     "+ColId);
                Fru fru = RepoManager.FruRepo.Single(f => f.Codice_Fru == "MOTIV00001");
                Pru_Col pruCol = RepoManager.Pru_ColRepo.Single(pr => pr.Pru_Id == pru.Pru_Id);
                Fru_Cant fruCant = RepoManager.Fru_CantRepo.Single(fc => fc.Fru_Id == fru.Fru_Id);
                Col col = RepoManager.ColRepo.Single(c => c.Col_Id == pruCol.Col_Id);
                Cant can = RepoManager.CantRepo.Single(c => c.Cant_Id == fruCant.Cant_Id);
                Tab_Decod motivazione = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == Justification && td.Nome_Tab == "MOTIVAZIONI");
                DateTime dataE = new DateTime(From.Year,From.Month,From.Day,Start.Hours,Start.Minutes,Start.Seconds);
                var dataPrimoGiorno = From.ToString().Split(' ');
                string noteReg = "" + dataPrimoGiorno[0];
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
                    Note_Reg = noteReg
                };
                regsToAdd.Add(regE);
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
                    Data_Registrazione_Reg = DateTime.UtcNow,
                    DataOraUltimaModifica_Reg = DateTime.UtcNow,
                    Motivazione_Reg_Id = motivazione.Tab_Decod_Id,
                    Note_Reg = noteReg
                };
                //creo le reg e le aggiungo ad una lista che aggiungerò al db
                regsToAdd.Add(regU);
                RepoManager.RegRepo.Add(regsToAdd, true);
                //una volta inserite le registrazioni a db le vado a recuperare così che abbiano ottenuto l'id univoco e possa procedere ad associarle
                regsToElaborate.Add(RepoManager.RegRepo.Single(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg == dataE));
                regsToElaborate.Add(RepoManager.RegRepo.Single(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg == dataU));
                RepoManager.RegRepo.Elaborate(regsToElaborate, From,new DateTime(From.Year,From.Month,From.Day,23,59,59),true,true);
            }
            else 
            {
                //url tipo http://localhost:55513//api/InsertRequest//?from=2025-02-24T00:00:00.00000000&to=2025-02-27T00:00:00.00000000&matricola=50044&justification=RIchiesta Ferie
                //se sono qui è una richiesta di una giornata intera
                List<Reg> regsToAdd = new List<Reg>();
                List<Reg> regsToElaborate = new List<Reg>();
                //in base ai dati ricevuti tramite parmetro recupero le matricole e i relativi cantieri e collaboratori associati
                Pru pru = RepoManager.PruRepo.Single(p => p.Codice_Pru == "     " + ColId);
                Fru fru = RepoManager.FruRepo.Single(f => f.Codice_Fru == "MOTIV00001");
                Pru_Col pruCol = RepoManager.Pru_ColRepo.Single(pr => pr.Pru_Id == pru.Pru_Id);
                Fru_Cant fruCant = RepoManager.Fru_CantRepo.Single(fc => fc.Fru_Id == fru.Fru_Id);
                Col col = RepoManager.ColRepo.Single(c => c.Col_Id == pruCol.Col_Id);
                Cant can = RepoManager.CantRepo.Single(c => c.Cant_Id == fruCant.Cant_Id);
                Tab_Decod motivazione = RepoManager.Tab_DecodRepo.Single(td => td.Decodifica_Tab == Justification && td.Nome_Tab == "MOTIVAZIONI");
                DateTime from = From;
                int regs = 1;
                string noteReg = "";
                while (from <= To) {
                    if (from.DayOfWeek != DayOfWeek.Saturday && from.DayOfWeek != DayOfWeek.Sunday) {
                        if (regs == 1) {
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
                        regsToAdd.Add(regE);
                    }
                    from = from.AddDays(1);
                }
                RepoManager.RegRepo.Add(regsToAdd, true);
                //una volta inserite le registrazioni a db le vado a recuperare così che abbiano ottenuto l'id univoco e possa procedere ad associarle
                regsToElaborate.AddRange(RepoManager.RegRepo.GetAllQueryable(r => r.Fru_Id == fru.Fru_Id && r.Pru_Id == pru.Pru_Id && r.Registrazione_Data_Ora_Fis_Reg >= From && r.Registrazione_Data_Ora_Fis_Reg <= To));
                RepoManager.RegRepo.Elaborate(regsToElaborate, From, To, true, true);
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
    }

}