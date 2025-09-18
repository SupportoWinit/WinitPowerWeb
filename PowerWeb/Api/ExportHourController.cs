using Business.DataClasses.WebApiDataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Business.Repository;
using Common;
using Domain;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spire.Xls;
using DevExpress.XtraScheduler;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using System.Web.Helpers;
using System.Text.RegularExpressions;

namespace PowerWeb.Api
{
    public class ExportHourController : GenericApi
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
            List<ExportOreForfait> returnList = new List<ExportOreForfait>();
            List<Cant> cantieriToExport = RepoManager.CantRepo.GetAllQueryable(c => c.Flag_NON_Esportare_Can.Value == 0).ToList();
            // Serialize the JObject to a JSON string
            string jsonString = "";
            _log.InfoFormat("Inizio a ciclare il periodo fornito: {0} - {1}", From.ToString(), To.ToString());
            // Controllo se ci sono cantieri per cui bisogna esportare le ore
            if (cantieriToExport.Count() > 0) 
            {
                Col collaboratoreElya = RepoManager.ColRepo.Single(c => c.Nome_Col == "ELYA");
                Col collaboratoreFiammetta = RepoManager.ColRepo.Single(c => c.Nome_Col == "FIAMMETTA");
                Col primoGovernante = RepoManager.ColRepo.Single(c => c.Livello_Col == "1");
                foreach (Cant cantiere in cantieriToExport)
                {
                    _log.InfoFormat("Inizio a ciclare per il cantiere {0}", cantiere.Descrizione_Can);
                    // Per ogni cantiere ciclo il periodo richiesto
                    DateTime date = From;
                    while (date <= To) 
                    {
                        _log.InfoFormat("Sto ciclando il giorno {0}", date.ToString());
                        string day = "";
                        if (date.Day < 10)
                        {
                            if (date.Month < 10)
                            {
                                day = "0" + date.Day + "/0" + date.Month + "/" + date.Year;
                            }
                            else
                            {
                                day = "0" + date.Day + "/" + date.Month + "/" + date.Year;
                            }
                        }
                        else
                        {
                            if (date.Month < 10)
                            {
                                day = "" + date.Day + "/0" + date.Month + "/" + date.Year;
                            }
                            else
                            {
                                day = "" + date.Day + "/" + date.Month + "/" + date.Year;
                            }
                        }
                        //recupero le timbrature fisse ovvero quelle di Elya, di Fiammetta e le coperture serali
                        List<Reg_V> regElya = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratoreElya.Col_Id && r.Data_Reg.Value == date && r.Cant_Id == cantiere.Cant_Id).ToList();
                        List<Reg_V> regFiammetta = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratoreFiammetta.Col_Id && r.Data_Reg == date && r.Cant_Id == cantiere.Cant_Id).ToList();
                        List<Reg_V> coperture = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Turno == "Coperture Serali" && r.Cant_Id == cantiere.Cant_Id && r.Data_Reg == date).ToList();
                        List<Reg_V> regPrimoGovernante = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == primoGovernante.Col_Id && r.Cant_Id == cantiere.Cant_Id && r.Data_Reg == date).ToList();
                        int totale = 0;
                        foreach (var reg in regElya) 
                        {
                            if (reg.Durata_Fig != null)
                                totale += reg.Durata_Fig.Value;
                        }
                        foreach (var reg in regFiammetta) 
                        {
                            if (reg.Durata_Fig != null)
                                totale += reg.Durata_Fig.Value;
                        }
                        foreach (var reg in coperture) 
                        {
                            if (reg.Durata_Fig != null)
                                totale += reg.Durata_Fig.Value;
                        }
                        // Controllo se il primo governante ha timbrature
                        if (regPrimoGovernante.Count() > 0)
                        {
                            foreach (var reg in regPrimoGovernante)
                            {
                                if (reg.Durata_Fig != null)
                                    totale += reg.Durata_Fig.Value;
                            }
                        }
                        else 
                        {
                            // In caso non ne abbia recupero il secondo governante e relative timbrature
                            Col secondoGovernante = RepoManager.ColRepo.Single(c => c.Livello_Col == "2");
                            List<Reg_V> regSecondoGovernante = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == secondoGovernante.Col_Id && r.Cant_Id == cantiere.Cant_Id && r.Data_Reg == date).ToList();
                            if (regSecondoGovernante.Count() > 0)
                            {
                                foreach (var reg in regSecondoGovernante)
                                {
                                    if (reg.Durata_Fig != null)
                                        totale += reg.Durata_Fig.Value;
                                }
                            }
                            else 
                            {
                                // Se anche il secondo non ne ha recupero le timbrature del terazo
                                Col terzoGovernante = RepoManager.ColRepo.Single(c => c.Livello_Col == "3");
                                List<Reg_V> regTerzoGovernante = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == terzoGovernante.Col_Id && r.Cant_Id == cantiere.Cant_Id && r.Data_Reg == date).ToList();
                                foreach (var reg in regTerzoGovernante)
                                {
                                    if (reg.Durata_Fig != null)
                                        totale += reg.Durata_Fig.Value;
                                }
                            }
                        }
                        //Aggiungo 4 ore che sono una quantità standard
                        totale += 240;
                        ExportOreForfait ore = new ExportOreForfait();
                        ore.codiceCantiere = cantiere.Codice_Gestionale_Can;
                        ore.totaleOre = (double)totale/60;
                        ore.data = day;
                        returnList.Add(ore);
                        date = date.AddDays(1);
                    }
                }
            }
            jsonString = JsonConvert.SerializeObject(returnList);
            JsonData = jsonString;
        }
    }
}