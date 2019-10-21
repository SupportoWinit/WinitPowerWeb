using Business.DataClasses;
using Business.Profile;
using Newtonsoft.Json;
using System;
using System.Web.Security;
using System.Web.Services;

namespace PowerWeb.Pages.Account
{
    public partial class RegisterPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }


        [WebMethod]
        public static string CreateUser(string userName, string password, string question, string answer)
        {

            if (PowerWebMembershipProvider.IsUserDuplicated(userName))
                return JsonConvert.SerializeObject(new LoginValidationResult() { isValid = false, validationError = "L'utente inserito esiste già!" });

            LoginValidationResult passwordValidation = PowerWebMembershipProvider.ValidatePassword(userName, password,false);

            if (passwordValidation.isValid)
            {
                PowerWebMembershipProvider.CreateAdminUser(userName, password, question, answer);
            }

            return JsonConvert.SerializeObject(passwordValidation);
        }

    }
}