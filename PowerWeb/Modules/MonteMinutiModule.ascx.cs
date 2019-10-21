using System;
using log4net;
using DevExpress.Web.ASPxCallback;
using System.Collections.Generic;
using Domain;
using Business.Repository;
using System.Linq;
using Newtonsoft.Json.Linq;
using Business.BusinessExtension;
using Common;

namespace PowerWeb.Modules
{
    public partial class MonteMinutiModule : BaseGridModule, IGeoLocationModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MonteMinutiModule));
        List<Col> colList;
        public JArray colArray;

        protected void Page_Init(object sender, EventArgs e)
        {
            colArray = new JArray();
            //Carica la lista dei collaboratori non diabilitati
            colList = RepoManager.ColRepo.GetAll().Where(c => !c.DisAbilitazione_Col).ToList();
            
            //Popola il JSON con le info dei collaboratori. Alla selezione della data, verrà attaccata l'informazione del monteminuti del mese selezionato.
            foreach (Col col in colList)
            {
                //Formatta il nome nel caso di clienti analfabeti
                string[] names = col.Nome_Col.Trim().Split(' ');
                for (int i = 0, z = names.Length; i < z; i++)
                {
                    if (names[i] != "")
                    {
                        names[i] = char.ToUpper(names[i][0]) + names[i].Substring(1).ToLower();
                    }
                }

                JObject colObj = new JObject();
                colObj.Add("ColId", col.Col_Id);
                colObj.Add("Codice_Collaboratore", col.Codice_Collaboratore);
                colObj.Add("Cognome_Col", col.Cognome_Col.ToUpper());
                colObj.Add("Nome_Col", string.Join(" ", names));

                colArray.Add(colObj);
            }

        }

        /// <summary>
        /// Callback innescato al cambio del mese selezionato.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="CallbackEventArgs"/> instance containing the event data.</param>
        protected void dateChanged_OnCallback(object source, CallbackEventArgs e)
        {
            string errorMessage = "";
            try {
                //Recupera la data selezionata dal relativo campo nascosto
                DateTime selectedDate = DateTime.ParseExact(selectedDateId.Value, "M/yyyy", null);
                string monteMinutiMonth = string.Format("M{0}_Col_Monte_Minuti", selectedDate.Month.ToString("00"));

                List<Col_Monte_Minuti> allMonteMinutiLine = RepoManager.Col_Monte_MinutiRepo.Find(m => m.Anno_Col_Monte_Minuti == selectedDate.Year).ToList();
                List<Col_Monte_Minuti> monteMinutiLine = new List<Col_Monte_Minuti>();

                foreach (Col_Monte_Minuti cmm in allMonteMinutiLine)
                {
                    if ((int)CommonService.GetPropertyValue(cmm, monteMinutiMonth) != TimesheetModuleItem.DEFAULT_MONTEMINUTI)
                    {
                        monteMinutiLine.Add(cmm);
                    }
                }

                IEnumerable<int> colIdList = monteMinutiLine.Select(m => (int)CommonService.GetPropertyValue(m, "Col_Id"));

                foreach (JObject col in colArray)
                {
                    int col_id = (int)col["ColId"];
                    string colMonteMinuti = "Non impostato";

                    if (colIdList.Contains(col_id))
                    {
                        colMonteMinuti = CommonService.GetHHMMStringFormMinutes((int)CommonService.GetPropertyValue(monteMinutiLine.First(mml => mml.Col_Id == col_id), monteMinutiMonth));
                    }

                    if (col["MonteMinuti_Col"] != null)
                    {
                        col.Add("MonteMinuti_Col", colMonteMinuti);
                    }
                    else
                    {
                        col["MonteMinuti_Col"] = colMonteMinuti;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(string.Format("{0}{1} - {2}", "Si è verirficato un errore nel recupero dei monteminuti di ", DateTime.ParseExact(selectedDateId.Value, "M/yyyy", null),  ex.ToString()));
                errorMessage = "Si è verirficato un errore nel recupero dei monteminuti, riprovare più tardi o contattare l'assistenza";
            }
            finally
            {
                dateChanged.JSProperties["cpErrorMessage"] = errorMessage;
            }

            dateChanged.JSProperties["cpMonteminutiArray"] = colArray.ToString();
        }

        protected void rowUpdate_OnCallback(object sender, CallbackEventArgs e)
        {
            string errorMessage = "";
            try
            {
                JObject rowObject = JObject.Parse(rowToUpdateId.Value);
                DateTime selectedDate = DateTime.ParseExact(selectedDateId.Value, "M/yyyy", null);
                int colId = (int)rowObject["ColId"];
                int monteMinuti = CommonService.GetMinutesNumberFromHHMMString((string)rowObject["MonteMinuti_Col"]);
                TimesheetModuleItem.updateMonteMinuti(colId, selectedDate, monteMinuti);
            }
            catch (Exception ex)
            {
                _log.Error(string.Format("Si è verirficato un errore nell'aggionramento del monteminuti di {0} del collaboratore {1} - {2}", DateTime.ParseExact(selectedDateId.Value, "M/yyyy", null).ToString(), (int)JObject.Parse(rowToUpdateId.Value)["ColId"], ex.ToString()));
                errorMessage = "Si è verificato un errore nell'aggionramente del monte minuti, riprovare più tardi o contattare l'assistenza.";
            }

            rowUpdate.JSProperties["cpErrorMessage"] = errorMessage;
        }
    }
}