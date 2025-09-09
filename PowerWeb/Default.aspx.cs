using System;
using System.IO;
using Business;
using Business.Repository;
using Common;
using Domain;
using log4net;

namespace PowerWeb
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            DateTime expiryDate = DateTime.MinValue;

            TimeSpan remainigDays = new TimeSpan(0);

            if (!File.Exists(Server.MapPath(@"\Scripts\debug.txt")))
            {
                if (PowerWebContext.Current.User.Codice_Utente != "APIUSER") 
                {
                    BusinessService.CheckFirstTimeInitializeLicence();

                    BusinessService.CheckFirstTimeIntializeModulesActivation();

                    BusinessService.CheckFirstTimeIntializeWinitPasswordUpdate();

                    var paramsRow = RepoManager.ParamRepo.ParametersRow;
                    expiryDate = BusinessService.GetExpirationDate(paramsRow);
                }
            }
            else
            {
                // se sono in debug allora imposto il max value sulla data di scadenza
                expiryDate = DateTime.MaxValue;
            }
            if (expiryDate.Date >= DateTime.UtcNow.Date)
                remainigDays = expiryDate.Date - DateTime.UtcNow.Date;

            lblExpireDate.Text = String.Format(BusinessService.GetLocalizedString(PowerWebResources.STR_DATA_SCADENZA), expiryDate.ToShortDateString());
            lblExpireDateWarning.Text = BusinessService.GetLocalizedString(PowerWebResources.STR_LICENZA_SCADUTA);

            if (remainigDays.Days > 0)
            {
                lblExpireDateWarning.Visible = false;
            }

            if (remainigDays.Days > 0 && remainigDays.Days <= Common.Properties.Settings.Default.GiorniAvvisoScadenza)
            {
                lblExpireDateWarning.Visible = true;
                lblExpireDateWarning.Text = String.Format(BusinessService.GetLocalizedString(PowerWebResources.STR_RIMANGONO_ANCORA_N_GIORNI_ALLA_SCADENZA_DELLA_LICENZA), remainigDays.Days);
            }

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            lblAzienda.Text = RepoManager.ParamRepo.ParametersRow.CompanyName;
        }

        protected void CompanyLogo_Init(object sender, EventArgs e)
        {
            CompanyLogo.Value = RepoManager.ParamRepo.ParametersRow.CompanyLogo;
        }
    }
}
