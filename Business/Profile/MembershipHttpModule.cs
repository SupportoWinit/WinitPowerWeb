using System;
using System.Web;
using Domain;
using Business.Repository;
using System.Web.Security;
using log4net;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using Common.Properties;
using System.Linq;
using Data;

namespace Business.Profile
{
    public class MembershipHttpModule : IHttpModule
    {
        public void Init(HttpApplication application)
        {
            //application.AuthenticateRequest += application_AuthenticateRequest;
            //application.AcquireRequestState += application_AcquireRequestState;
            application.PostAcquireRequestState += application_PostAcquireRequestState;
            application.AcquireRequestState += application_AcquireRequestState;
        }

        void application_AcquireRequestState(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            log4net.ThreadContext.Properties["UserHostName"] = context.Request.UserHostName;

            if (PowerWebContext.Current.Lingua != null)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(PowerWebContext.Current.Lingua.Sigla_Lingue);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(PowerWebContext.Current.Lingua.Sigla_Lingue);
            }

        }

        private void application_PostAcquireRequestState(object sender, EventArgs e)
        {
            if (!((HttpApplication)sender).Context.Request.Path.EndsWith("aspx"))
                return;

            if (!PowerWebConfig.IsConnectionStringSet)
                return;

            bool authenticated = false;
            if (HttpContext.Current.User != null && HttpContext.Current.User.Identity != null)
                authenticated = HttpContext.Current.User.Identity.IsAuthenticated;

            if (authenticated)
            {
                if (PowerWebContext.Current.User == null)
                {
                    Utenti currentUser = RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == HttpContext.Current.User.Identity.Name);
                    if (currentUser != null)
                        PowerWebMembershipProvider.InitializeUser(currentUser);
                    else
                        PowerWebContext.LogOut();
                }

                if (!File.Exists(HttpContext.Current.Server.MapPath(@"\Scripts\debug.txt")))
                {
                    if (Business.BusinessService.CheckFirstTimeInitializeLicence())
                        PowerWebContext.LogOut();
                }
            }
            else
                PowerWebContext.Current.User = null;
        }

        public void Dispose()
        {
        }
    }
}
