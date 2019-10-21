using Business;
using Business.Infrastructure;
using Business.Repository;
using Business.Repository.Custom;
using Common;
using DevExpress.Web.ASPxCallback;
using Domain;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace PowerWeb
{
    
    public partial class GridWrapperPage : System.Web.UI.MasterPage
    {
        private static readonly ILog _log = LogManager.GetLogger("GridWrapperPage");

        public static JArray viste;
        public int currentLayoutId;
        public JObject bingKey;
        public JObject initVista;

        public string page;
        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Page.IsCallback && !Page.IsPostBack)
            {
                page = this.Page.Request.FilePath.Split('/')[2];

                List<Tab_DataGrid> views = RepoManager.Tab_DataGridRepo.Find(vista => vista.Nome_DataGrid == page).OrderBy(data => data.Data_Layout_DataGrid).ToList();
                
                if (views.Count == 0)
                {

                }
                else
                {
                    viste = JArray.FromObject(views);
                    initVista = (JObject)viste[17];
                }

                bingKey = new JObject();
                bingKey.Add("key",RepoManager.ParamRepo.ParametersRow.BingKey);
            }
        }

        /*Ogni volta che viene richiamata aggiorna la lista di viste del selectBox
          e ricerca la vista più recente*/

        public void BindLayoutCombo(bool isSetToDefault = false)
        {
            List<Tab_DataGrid> listLayout = RepoManager.Tab_DataGridRepo.Find(tdg => tdg.Nome_DataGrid == Page.Request.FilePath && (tdg.Utenti_Id == PowerWebContext.Current.User.Utenti_Id || tdg.Utenti_Id == null)).OrderBy(tgd => tgd.Nome_Layout).ToList();

            Tab_DataGrid currentLayout = null;
            Tab_DataGrid currentAdminLayout = null;

            //Lista di opzioni layout da passare alla selectBox
            viste = new JArray();

            //Singolo layout
            JObject vistItem;

            foreach (Tab_DataGrid item in listLayout)
            {
                //Ricerca nel database qual è l'ultimo layout utilizzato
                if (item.Utenti_Id == null)
                {
                    if (currentAdminLayout == null)
                        currentAdminLayout = item;

                    else if (item.Data_Layout_DataGrid > currentAdminLayout.Data_Layout_DataGrid)
                        currentAdminLayout = item;
                }
                else
                {
                    //
                    if (currentLayout == null)
                        currentLayout = item;
                    else if (item.Data_Layout_DataGrid > currentLayout.Data_Layout_DataGrid)
                        currentLayout = item;
                }


                vistItem = new JObject();
                vistItem.Add("nome", item.Nome_Layout);
                vistItem.Add("state", item.Layout_DataGrid);
                vistItem.Add("id", item.DataGrid_Id);

                viste.Add(vistItem);


            }
            
            if (currentLayout == null)
                currentLayout = currentAdminLayout;

            currentLayoutId = listLayout.IndexOf(currentLayout);
            selectBoxDataSource.Value = viste.ToString();
        }

        public static string getPageViews(string page)
        {

            List<Tab_DataGrid> views = RepoManager.Tab_DataGridRepo.Find(v => v.Nome_DataGrid == page).ToList();
            if (views.Count > 0)
            {
                JArray dataSource = new JArray();
                foreach(var it in views)
                {
                    JObject view = JObject.FromObject(it);
                    view.Remove("Layout_DataGrid");
                    view.Add("Layout_DataGrid",JObject.Parse(it.Layout_DataGrid));
                    dataSource.Add(view);
                }
                 var a = JsonConvert.SerializeObject(dataSource, Formatting.None);
                return JsonConvert.SerializeObject(dataSource,Formatting.None);
            }
            return null;
        }

        public static string savePageViews(JObject view, string page)
        {
            JObject result = new JObject();
            Tab_DataGrid recordToInsert = null;
            

            string stato = view["stato"].ToString(Formatting.None);
            string nome = view["nome"].ToString();

            string errorMessage = "";

            if (nome.Trim() != string.Empty)
            {
                if (nome.ToUpper() != "DEFAULT")
                {

                    int? idUtente = PowerWebContext.Current.User.Utenti_Id;
                    if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                    //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine salvato SENZA UTENTE
                    {
                        idUtente = null;
                    }
                    Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == page
                       && tdg.Nome_Layout == nome && tdg.Utenti_Id == null);



                    if (savedLayout == null)
                    {
                        try
                        {
                            recordToInsert = new Tab_DataGrid
                            {
                                Utenti_Id = idUtente,
                                Nome_DataGrid = page,
                                Layout_DataGrid = stato,
                                Nome_Layout = nome,
                                Data_Layout_DataGrid = DateTime.UtcNow,

                            };
                            RepoManager.Tab_DataGridRepo.Add(recordToInsert, true);
                            result.Add("state", "Done");
                            errorMessage = "Salvataggio della vista avvenuto con successo";
                            result.Add("message", errorMessage);
                        }
                        catch (Exception ex)
                        {
                            result.Add("state", "Fail");
                            errorMessage = "Errore durante il salvataggio della vista, riprovare più tardi";
                            result.Add("message", errorMessage);
                            _log.Error("Errore durante il salvataggio della vista " + nome + ":  " + ex.ToString());
                        }
                    }
                    else
                    {
                        savedLayout.Layout_DataGrid = stato;
                        savedLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                        try
                        {
                            RepoManager.Tab_DataGridRepo.Update(savedLayout, true);
                            errorMessage = "Modifica della vista avvenuto con successo";
                            result.Add("state", "Done");
                            result.Add("message", errorMessage);
                            

                        }
                        catch (Exception ex)
                        {
                            result.Add("state", "Fail");
                            errorMessage = "Errore durante l'aggiornamento della vista, riprovare più tardi";
                            result.Add("message", errorMessage);
                            _log.Error("Errore durante l'aggiornamento della vista " + nome + ":  " + ex.ToString());
                        }
                    }
                }
                else
                //NESSUNO PUO MEMORIZZARE UNA DATAGRID con il NOME "DEFAULT"
                {
                    result.Add("state", "Fail");
                    errorMessage = "Non si puo salvare una vista con il nome DEFAULT";
                    result.Add("message", errorMessage);
                }

            }
            return JsonConvert.SerializeObject(result,Formatting.None);
        }

        public static string removePageViews(int view_Id, string page)
        {
            string message = "";
            JObject result = new JObject();

            Tab_DataGrid toDeleteLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.DataGrid_Id == view_Id);
            
            if (toDeleteLayout == null)
            {
                message = "Nessuna vista salvata con questo nome";
                result.Add("state", "Fail");
                result.Add("message",message);
            }
            else
            {
                try
                {
                    RepoManager.Tab_DataGridRepo.Delete(toDeleteLayout, true);
                    message = "Vista eliminata con successo";
                    result.Add("state", "Done");
                    result.Add("message", message);
                    result.Add("id", toDeleteLayout.DataGrid_Id);
;                }
                catch (Exception ex)
                {
                    message = "Errore durante la cancellazione della vista, riprovare più tardi";
                    result.Add("state", "Fail");
                    
                }

            }
            return JsonConvert.SerializeObject(result, Formatting.None);
        }

        /*Salva vista a database con relativi controlli */


        protected  void saveGridState_OnCallback(object source, CallbackEventArgs e)
        {

            string s = e.Parameter;
            JObject tmp = JObject.Parse(s);

            string stato = tmp["stato"].ToString();
            string testo = (String)tmp["nome"];

            string errorMessage = "";

            if (testo.Trim() != string.Empty)
            {
                if (testo.ToUpper() != "DEFAULT")
                {

                    int? idUtente = PowerWebContext.Current.User.Utenti_Id;
                    if (PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level)
                    //Se l'utente ha un Livello >= al Livello di Admin definito in Tab Param allora il Layout veine salvato SENZA UTENTE
                    {
                        idUtente = null;
                    }
                    Tab_DataGrid savedLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_DataGrid == Page.Request.FilePath
                       && tdg.Nome_Layout == testo && tdg.Utenti_Id == null);



                    if (savedLayout == null)
                    {
                        try
                        {
                            RepoManager.Tab_DataGridRepo.Add(new Tab_DataGrid
                            {
                                Utenti_Id = idUtente,
                                Nome_DataGrid = Page.Request.FilePath,
                                Layout_DataGrid = stato,
                                Nome_Layout = testo,
                                Data_Layout_DataGrid = DateTime.UtcNow,

                            }, true);
                        }
                        catch (Exception ex)
                        {
                            errorMessage = "Errore durante il salvataggio della vista, riprovare più tardi";
                            _log.Error("Errore durante il salvataggio della vista " + testo + ":  " + ex.ToString());
                        }




                    }
                    else
                    {
                        savedLayout.Layout_DataGrid = stato;
                        savedLayout.Data_Layout_DataGrid = DateTime.UtcNow;
                        try
                        {
                            RepoManager.Tab_DataGridRepo.Update(savedLayout, true);
                        }
                        catch (Exception ex)
                        {
                            errorMessage = "Errore durante l'aggiornamento della vista, riprovare più tardi";
                            _log.Error("Errore durante l'aggiornamento della vista " + testo + ":  " + ex.ToString());
                        } 
                    }
                }
                else
                //NESSUNO PUO MEMORIZZARE UNA DATAGRID con il NOME "DEFAULT"
                {

                    errorMessage = "Non si puo salvare una vista con il nome DEFAULT";
                }
                
            }
            BindLayoutCombo(true);
            //saveGridState.JSProperties["cpErrorMessage"] = errorMessage;
            //saveGridState.JSProperties["cpDataSource"] = viste.ToString();
            //saveGridState.JSProperties["cpCurrentLayout"] = currentLayoutId;
        }

        //Elimina la vista inserita nel selectBox dal database
        protected void deleteGridState_OnCallback(object source, CallbackEventArgs e)
        {
            
            JObject tmp = JObject.Parse(e.Parameter);
            string nome = tmp["nome"].ToString();
            string errorMessage = "";

            Tab_DataGrid toDeleteLayout = RepoManager.Tab_DataGridRepo.FirstOrDefault(tdg => tdg.Nome_Layout == nome);

            System.Diagnostics.Debug.WriteLine(toDeleteLayout);
            if (toDeleteLayout == null)
            {
                errorMessage = "Nessuna vista salvata con questo nome";
            }
            else
            {
                try
                {
                    RepoManager.Tab_DataGridRepo.Delete(toDeleteLayout, true);
                }
                catch (Exception ex)
                {
                    errorMessage = "Errore durante la cancellazione della vista, riprovare più tardi";
                    _log.Error("Errore durante la cancellazione della vista " + nome + " nella pagina "+ Page.Request.FilePath+" " + ex.ToString());
                }

            }
            BindLayoutCombo(true);
            //deleteGridState.JSProperties["cpErrorMessage"] = errorMessage;
            //deleteGridState.JSProperties["cpDataSource"] = viste.ToString();
            //deleteGridState.JSProperties["cpCurrentLayout"] = currentLayoutId;

        }
        
        protected void saveViewDate_OnCallback(object source, CallbackEventArgs e)
        {
            DateTime current = DateTime.UtcNow;
            int id = Convert.ToInt32(e.Parameter);
            
            try
            {
                Tab_DataGrid vista = RepoManager.Tab_DataGridRepo.FirstOrDefault(o => o.DataGrid_Id == id);
                vista.Data_Layout_DataGrid = current;
                RepoManager.Tab_DataGridRepo.Update(vista, true);
                System.Diagnostics.Debug.WriteLine(current);
            }
            catch (Exception ex) {
                _log.Error("Errore durante l'assegnazione della data alla vista selezionata:  " + ex.ToString());
            }
            
        }

        

        
    }
}