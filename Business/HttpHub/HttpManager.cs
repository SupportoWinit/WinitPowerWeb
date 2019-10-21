using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Business.HttpHub
{
    public class HttpManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HttpManager));

        public string Host { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }

        public void PostRequest(JObject payload)
        {
            var client = new HttpClient();

            try
            {

                UriBuilder baseUriBuilder = new UriBuilder(Host);
                baseUriBuilder.Path = Path;

                baseUriBuilder.Port = int.Parse(Port);

                client.BaseAddress = baseUriBuilder.Uri;


                var stringContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.None);
                var buffer = Encoding.UTF8.GetBytes(stringContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                _log.InfoFormat("Stringa di connessione : {0}", baseUriBuilder.ToString());

                _log.InfoFormat("Inizio chiamata asincrona ClockAppsManager al controller {0}", baseUriBuilder.Path);

                Task.Run(() =>
                {
                    client.PostAsync(baseUriBuilder.Uri, byteContent).ContinueWith(r =>
                    {
                        _log.InfoFormat("Status risposta httpManager: {0}", r.Status);
                        _log.InfoFormat("ReasonPhrase risposta httpManager: {0}", r.Result.ReasonPhrase);

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

    }
}
