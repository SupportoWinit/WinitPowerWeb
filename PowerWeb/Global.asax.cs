using Business;
using Business.Infrastructure;
using log4net;
using Microsoft.AspNet.WebFormsDependencyInjection.Unity;
using System;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.SessionState;

namespace PowerWeb
{
    public class Global : System.Web.HttpApplication
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

        void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            //log4net.ThreadContext.Properties["UserHostName"] = Request.UserHostName;
        }

        void Application_BeginRequest(object sender, EventArgs e)
        {
            PowerWebConfig.Init();

        }
        void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }
        void Application_Start(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("it-IT");

            Bootstrap.Bootstrap.InitializeContainer(this.AddUnity());

            // impostazione delle ricerche devexpress per griglie linq case insensitive
            DevExpress.Data.Helpers.ServerModeCore.DefaultForceCaseInsensitiveForAnySource = true;

            // Codice eseguito all'avvio dell'applicazione
            log4net.Config.XmlConfigurator.Configure();

            PowerWebConfig.Init();

            RouteTable.Routes.MapHttpRoute(
               name: "PowerWebAPI",
               routeTemplate: "api/{controller}/{action}/{id}",
               defaults: new { action = RouteParameter.Optional, id = RouteParameter.Optional }
               );

            if (PowerWebConfig.IsConnectionStringSet)
            {
                IoC.InitializeWith(new DependencyResolverFactory());
            }


        }

        void Application_End(object sender, EventArgs e)
        {
            _log.InfoFormat("Applicazione riciclata o distrutta : {0}", nameof(Application_End));

        }

        void Application_Destroyed(object sender, EventArgs e)
        {
            _log.InfoFormat("Applicazione riciclata o distrutta : {0}", nameof(Application_Destroyed));
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (ex.InnerException != null)
                ex = Common.CommonService.GetInternalException(ex);

            if (ex == null || ex is ThreadAbortException)
                return;

            _log.ErrorFormat("Errore non gestito!");

            _log.ErrorFormat("Tipo exception: {0}", ex.GetType().Name);

            _log.ErrorFormat("Messaggio exception: {0}", ex.Message);

            _log.ErrorFormat("Tipo exception: {0}", ex.GetType().Name);

            _log.ErrorFormat(ex.StackTrace);
        }

        void Session_Start(object sender, EventArgs e)
        {

        }

        void Session_End(object sender, EventArgs e)
        {
            // Codice eseguito al termine di una sessione. 
            // Nota: l'evento Session_End viene generato solo quando la modalità sessionstate
            // è impostata su InProc nel file Web.config. Se la modalità è impostata su StateServer 
            // o SQLServer, l'evento non viene generato.

        }

        protected void Application_PostAuthorizeRequest()
        {
            if (IsWebApiRequest())
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);

        }

        private bool IsWebApiRequest()
        {
            return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(WebApiConfig.UrlPrefixRelative);
        }

    }

    public static class WebApiConfig
    {
        public static string UrlPrefix { get { return "api"; } }
        public static string UrlPrefixRelative { get { return "~/api"; } }

        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
            name: "PowerWebAPI",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
