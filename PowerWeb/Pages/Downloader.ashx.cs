
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace PowerWeb.Pages
{
    /// <summary>
    /// Classe di supporto per il download di file salvati su filesystem
    /// </summary>
    public class Downloader : IHttpHandler, IRequiresSessionState
    {
        private bool delete = true;

        public void ProcessRequest(HttpContext context)
        {
            var dict = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
            var json = new JavaScriptSerializer().Serialize(dict.AllKeys.ToDictionary(k => k, k => dict[k]));
            JObject requestPayLoad = JObject.Parse(json);


            string filePath = requestPayLoad["path"].ToString();
            string fileName = requestPayLoad["fileName"].ToString();
            string extension = requestPayLoad["extension"].ToString().Trim();

            if (requestPayLoad["delete"] != null)
                delete = false;

            string completePath = string.Format("{0}{1}.{2}", filePath, fileName, extension);
            try
            {
                using (FileStream stream = new FileStream(completePath, FileMode.Open))
                {

                    context.Response.Clear();
                    context.Response.Buffer = true;

                    context.Response.AddHeader("content-disposition", String.Format("attachment;filename={0}", fileName + "." + extension));
                    context.Response.ContentEncoding = Encoding.UTF8;

                    context.Response.Cache.SetCacheability(HttpCacheability.Private);
                    context.Response.ContentType = GetContentType(extension);

                    stream.CopyTo(context.Response.OutputStream);
                    context.Response.Flush();
                    context.Response.End();
                }
            }
            finally
            {
                if (delete)
                    File.Delete(completePath);
            }
        }

        private string GetContentType(string extension)
        {
            string type = "";

            switch (extension)
            {
                case "txt":
                    type = "text/plain";
                    break;
                case "zip":
                    type = "application/zip";
                    break;
                case "xlsx":
                    type = "application/vnd.ms-excel";
                    break;
                case "pdf":
                    type = "application/pdf";
                    break;
                case "jpg":
                    type = "image/jpeg";
                    break;
                default:
                    type = "text/plain";
                    break;
            }

            return type;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}