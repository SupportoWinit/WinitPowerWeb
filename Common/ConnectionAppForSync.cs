using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Questa classe permette la sincronizzazione di un entità con il database di ClockAppsManager
    /// </summary>
    public class ClockAppsSyncHub
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ClockAppsSyncHub));

        #region PUBLIC PROPERTIES

        public string Path;

        public string ConnectionUrl;

        public int Port;

        #endregion

        #region  PRIVATE PROPERTIES

        private JObject _Content;

        private int _FirstId;

        private int _LastId;

        #endregion

        #region PUBLIC FUNCTIONS

        /// <summary>
        /// Esegue una chiamata asincrona di tipo POST al server ClockAppsManager al quale viene
        /// inviato un vettore di entità per la sincronizzazione con il relativo operatore
        /// (add,modify,delete)
        /// </summary>
        public void ExecutePost()
        {

            //Non viene usata la direttiva "using" perchè il client viene deallocato prima del termine dell'operazione Task.Run.
            //infatti quest'ultimo viene manualmente deallocato al termine del processo ( client.dispose() )
            var client = new HttpClient();

            try
            {

                UriBuilder baseUriBuilder = new UriBuilder(ConnectionUrl);
                baseUriBuilder.Port = Port;
                baseUriBuilder.Path = Path;

                client.BaseAddress = baseUriBuilder.Uri;


                var stringContent = Newtonsoft.Json.JsonConvert.SerializeObject(_Content, Newtonsoft.Json.Formatting.None);
                var buffer = Encoding.UTF8.GetBytes(stringContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                _log.InfoFormat("Inizio chiamata asincrona ClockAppsManager al controller {0}", baseUriBuilder.Path);

                Task.Run(() =>
                {
                    client.PostAsync(baseUriBuilder.Uri, byteContent).ContinueWith(r =>
                   {
                       HttpStatusCode responseCode = r.Result.StatusCode;

                       if (responseCode == HttpStatusCode.OK)
                       {
                           _log.Info("Sincronizzazione entità clockappManager completata con successo");
                       }
                       else
                       {
                           _log.Info("Sincronizzazione entità clockappManager fallita");
                           _log.Error(r.Result.Content.ReadAsStringAsync().Result);
                       }

                       client.Dispose();

                   });
                });

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la sincronizzazione di un entità con exception {0}", ex.Message);
            }

        }

        /// <summary>
        /// Esegue la sincronizzazione completa di una tabella di PowerWeb con la relativa tabella di ClockAppsManager
        /// </summary>
        public void ExecuteTotalSync()
        {


            var client = new HttpClient();

            try
            {

                UriBuilder baseUriBuilder = new UriBuilder(ConnectionUrl);
                baseUriBuilder.Port = Port;
                baseUriBuilder.Path = Path;

                client.BaseAddress = baseUriBuilder.Uri;


                var stringContent = Newtonsoft.Json.JsonConvert.SerializeObject(_Content, Newtonsoft.Json.Formatting.None);
                var buffer = Encoding.UTF8.GetBytes(stringContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                _log.Info("Inizio chiamata asincrona ClockAppsManager");

                Task.Run(() =>
                {
                    client.PutAsync(baseUriBuilder.Uri, byteContent).ContinueWith(r =>
                    {
                        HttpStatusCode responseCode = r.Result.StatusCode;

                        if (responseCode == HttpStatusCode.OK)
                        {
                            JObject response = JObject.Parse(r.Result.Content.ReadAsStringAsync().Result);

                            if (response["firstId"] != null)
                            {
                                _FirstId = int.Parse(response["firstId"].ToString());
                            }

                            if (response["lastId"] != null)
                            {
                                _LastId = int.Parse(response["lastId"].ToString());
                            }

                            _log.Info("Sincronizzazione cantieri completata con successo");
                        }
                        else
                        {
                            _log.Error("Sincronizzazione cantieri fallita. Controllare log ClockAppsManager");
                        }

                        client.Dispose();

                    });
                });

            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
            }

        }

        public void ConfirmSync()
        {
            var client = new HttpClient();

            try
            {

                UriBuilder baseUriBuilder = new UriBuilder(ConnectionUrl);
                baseUriBuilder.Port = Port;
                baseUriBuilder.Path = Path;

                client.BaseAddress = baseUriBuilder.Uri;


                _Content.Add("firstId", _FirstId);
                _Content.Add("lastId", _LastId);

                var stringContent = Newtonsoft.Json.JsonConvert.SerializeObject(_Content, Newtonsoft.Json.Formatting.None);
                var buffer = Encoding.UTF8.GetBytes(stringContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                _log.Info("Inizio chiamata asincrona ClockAppsManager");

                Task.Run(() =>
                {
                    client.PutAsync(baseUriBuilder.Uri, byteContent).ContinueWith(r =>
                    {
                        HttpStatusCode responseCode = r.Result.StatusCode;

                        if (responseCode == HttpStatusCode.OK)
                        {
                            _log.Info("Sincronizzazione cantieri completata con successo");

                            _FirstId = _LastId = 0;
                        }
                        else
                        {
                            _log.Error("Sincronizzazione cantieri fallita. Controllare log ClockAppsManager");
                        }

                        client.Dispose();

                    });
                });



            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
            }
        }

        /// <summary>
        /// Setta il percorso del controller di clockAppsManager e il body (JSON) da allegare alla POST
        /// </summary>
        public void Set(JObject content)
        {
            _Content = content;
        }

        #endregion

    }
}
