using System;
using System.Web.UI;
using Domain;
using System.Threading;
using System.Globalization;

namespace PowerWeb.Pages
{
    public class BasePage : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            if (PowerWebContext.Current == null || PowerWebContext.Current.User == null)
                Response.Redirect(PowerWebService.LoginPageURL);
        }

        protected override void InitializeCulture()
        {
            base.InitializeCulture();

            if (PowerWebContext.Current.Lingua != null)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(PowerWebContext.Current.Lingua.Sigla_Lingue);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(PowerWebContext.Current.Lingua.Sigla_Lingue);
            }
        }
    }
}