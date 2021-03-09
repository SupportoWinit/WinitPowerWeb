using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Business.HttpHub
{
    public class FlutterAppHttpModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HttpModule));

        public virtual string Host { get; set; }
        public virtual string Path { get; set; }
        public virtual string Port { get; set; }
        public virtual string Protocol { get; set; }


        public T Get<T>(JObject request, string path) where T : class
        {
            using (var client = new HttpClient())
            {
                T result = null;

                var uriBuilder = new UriBuilder();
                uriBuilder.Host = Host;
                uriBuilder.Path = path;
                uriBuilder.Port = 81;
                uriBuilder.Scheme = Protocol ?? "http";

                uriBuilder.Query = GetQueryString(request);

                try
                {
                    HttpResponseMessage response = client.GetAsync(uriBuilder.Uri).Result;

                    if (response.IsSuccessStatusCode)
                    {                        
                        result = JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        throw new Exception(String.Format("Errore chiamata {0} con statusCode {1} e reasonPhrase {2}", uriBuilder.Uri, response.StatusCode, response.ReasonPhrase));
                    }

                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Errore durante una chiamata asincrona (GET) : {0}", ex.Message);
                }

                return result;
            }
        }

        public void Post(JObject request, string path, string query = "")
        {
            using (var client = new HttpClient())
            {
                var uriBuilder = new UriBuilder();
                uriBuilder.Host = Host;
                uriBuilder.Path = path;
                uriBuilder.Scheme = Protocol ?? "https";

                _log.Info($"Porta settata : {Port}");

                if (Port != null)
                    uriBuilder.Port = int.Parse(Port);

                uriBuilder.Query = query;

                var stringContent = new StringContent(request.ToString());

                stringContent.Headers.ContentType.MediaType = "application/json";
                try
                {
                    HttpResponseMessage response = client.PostAsync(uriBuilder.Uri, stringContent).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        _log.InfoFormat("Chiamata effettuata con successo : uri= {0}, statusCode= {1}", uriBuilder.Uri, response.StatusCode);
                    }
                    else
                    {
                        throw new Exception(String.Format("Errore chiamata {0} con statusCode {1} e reasonPhrase {2}", uriBuilder.Uri, response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Errore durante una chiamata asincrona (POST) : {0}", ex.Message);
                }
            }
        }

        private string GetQueryString(JObject json)
        {
            var result = new List<string>();

            foreach (var prop in json)
            {
                var value = prop.Value;

                result.Add(string.Format("{0}={1}", prop.Key, HttpUtility.UrlEncode(value.ToString())));
            }

            return String.Join("&", result.ToArray());
        }
    }
}

