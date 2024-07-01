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
    public class ExportController : GenericApi
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
            List<JArray> list = new List<JArray>();
            List<ExportJson> jsons = new List<ExportJson>();
            // Serialize the JObject to a JSON string
            string jsonString = "";// jsonObject.ToString();
            //jsonString = JsonConvert.SerializeObject(jsonObject);
            DateTime date = From;
            while (date <= To) {
                var regVs = RepoManager.Reg_VRepo.GetAll().Where(r => r.Data_Ora_Fig_E.Value.Day == date.Day && r.Data_Ora_Fig_E.Value.Month == date.Month && r.Data_Ora_Fig_E.Value.Year == date.Year && r.Codice_Commessa_Can == "Hotel" && r.Registrazione_Stato_Reg != (int)RegStateEnum.None).OrderBy(r => r.Data_Ora_Fig_E).GroupBy(r => r.Col_Id).ToList();
                string day = "";
                if (date.Day < 10)
                {
                    if (date.Month < 10)
                    {
                        day = "0" + date.Day + "/0" + date.Month + "/" + date.Year;
                    }
                    else {
                        day = "0" + date.Day + "/" + date.Month + "/" + date.Year;
                    }
                    
                }
                else {
                    if (date.Month < 10)
                    {
                        day = "" + date.Day + "/0" + date.Month + "/" + date.Year;
                    }
                    else {
                        day = "" + date.Day + "/" + date.Month + "/" + date.Year;
                    }
                }
                
                JArray jsonArray = new JArray();
                
                foreach (var regs in regVs)
                {
                    string tmpCol = "";
                    string tmpCant = "";
                    int tmpDurata = 0;
                    int dayTimb = 1;
                    List<Col> collaboratore = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == regs.Key).ToList();
                    string col = collaboratore.First().Matricola_Col;
                    if (col == null) {
                        col = collaboratore.First().CognomeNome_Col;
                    }
                    foreach (Reg_V regv in regs){
                        if (dayTimb < regs.Count())
                        {
                            if (tmpCol == "" && tmpCant == "" && tmpDurata == 0)
                            {
                                tmpCol = col;
                                tmpCant = regv.Codice_Gestionale_Can;
                                tmpDurata = regv.Durata_Fig.Value;
                            }
                            else if (tmpCant == regv.Codice_Gestionale_Can)
                            {
                                tmpDurata += regv.Durata_Fig.Value;
                            }
                            else if(tmpCant != regv.Codice_Gestionale_Can)
                            {
                                string durata = oreCentesimi(tmpDurata);
                                ExportJson tmp = new ExportJson();
                                tmp.Durata = durata;
                                tmp.CantId = tmpCant;
                                tmp.ColId = tmpCol;
                                tmp.Data = day;
                                tmp.Serale = 0;
                                jsons.Add(tmp);
                                tmpDurata = 0;
                                tmpCant = regv.Codice_Gestionale_Can;
                            }
                            dayTimb++;
                        }
                        else
                        {
                            if (tmpCant != regv.Codice_Gestionale_Can)
                            {
                                // Create a JArray  
                                string durata = oreCentesimi(regv.Durata_Fig.Value);
                                ExportJson tmp = new ExportJson();
                                tmp.Durata = durata;
                                tmp.CantId = regv.Codice_Gestionale_Can;
                                tmp.ColId = col;
                                tmp.Data = day;
                                tmp.Serale = 0;
                                jsons.Add(tmp);
                            }
                            else
                            {
                                tmpDurata += regv.Durata_Fig.Value;
                                string durata = oreCentesimi(tmpDurata);
                                ExportJson tmp = new ExportJson();
                                tmp.Durata = durata;
                                tmp.CantId = regv.Codice_Gestionale_Can;
                                tmp.ColId = col;
                                tmp.Data = day;
                                tmp.Serale = 0;
                                jsons.Add(tmp);
                            }
                        }
                    }                           
                }
                var rtn = JsonConvert.SerializeObject(jsons);
                jsonString = jsonString + rtn;
                date = date.AddDays(1);
            }
            //var jsonObject1 = JsonConvert.DeserializeObject(jsonString);
            // Output the JSON string
            //string test = jsonObject1.ToString();
            Console.WriteLine(jsonString);
            JsonData = jsonString;
        }

        public string oreCentesimi(int minuti) {
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