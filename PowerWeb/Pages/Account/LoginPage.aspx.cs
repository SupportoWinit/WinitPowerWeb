using Business.BusinessServices.UserService;
using Business.DataClasses;
using Business.Infrastructure;
using Business.Profile;
using Business.Repository;
using Common;
using Common.Models.Login;
using log4net;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web.Security;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace PowerWeb.Pages.Account
{
    public partial class LoginPage : System.Web.UI.Page
    {
        public static readonly ILog _log = LogManager.GetLogger(typeof(Login));

        static IUserService _userService;
        
        protected void Page_Init(object sender, EventArgs e)
        {

        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
        
        public LoginPage(IUserService userService)
        {
            _userService = userService;
        }

        #region WEBMETHODS

        [WebMethod]
        public static string ValidateLogin(LoginRequest loginRequest)
        {
            LoginValidationResult result = _userService.Login(loginRequest);

            return JsonConvert.SerializeObject(result);
        }

        [WebMethod]
        public static string UpdateMail(string userName, string passWord, string newPassWord, string secretQuestion, string secretAnswer)
        {
            LoginValidationResult result = null;

            var user = Membership.GetUser(userName);

            if (user == null)
            {
                result = InvalidPasswordResponse(userName);
            }
            else if (!CheckUserPasswordEquality(user, passWord))
            {
                result = InvalidPasswordResponse(userName);
            }
            else
            {
                string error = "";

                result = PowerWebMembershipProvider.ValidatePassword(userName, newPassWord);

                if (result.isValid)
                {
                    result.isValid = PowerWebMembershipProvider.UpdatePassWord(userName, newPassWord, ref error);

                    PowerWebMembershipProvider.SetUserSecretQuestion(userName, secretQuestion);
                    PowerWebMembershipProvider.SetUserSecretAnswer(userName, secretAnswer);

                    result.validationError = String.IsNullOrEmpty(error) ? null : error;
                }

            }

            return JsonConvert.SerializeObject(result);
        }

        [WebMethod]
        public static string ChangePassword(string userName)
        {
            var user = Membership.GetUser(userName);

            if (user == null || RepoManager.UtentiRepo.DbSet.Find(user.ProviderUserKey).Liv_Utente < Common.Properties.Settings.Default.Admin_Level)
                return "false";

            return "true";

        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Ritorna risposta con messaggio in caso di username errato
        /// </summary>
        /// <param name="user">Il codice utente.</param>
        /// <returns></returns>
        private static LoginValidationResult InvalidUsernameResponse(string user)
        {
            LoginValidationResult result = new LoginValidationResult() { isValid = false };
            result.AddFormErrors("passWord", "Attenzione,l'utente inserito non esiste!");

            _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; Il nome utente è inesistente", user);

            return result;
        }

        /// <summary>
        ///  Ritorna risposta con messaggio in caso di password errata
        /// </summary>\
        /// <param name="user">Il codice utente.</param>
        /// <returns></returns>
        private static LoginValidationResult InvalidPasswordResponse(string user)
        {
            LoginValidationResult result = new LoginValidationResult() { isValid = false };
            result.AddFormErrors("passWord", "Attenzione,la password inserita è errata!");

            _log.InfoFormat("Tentativo di login fallito da parte dell'utente {0}; La password è errata", user);

            return result;
        }

        /// <summary>
        /// Controlla l'uguaglianza tra la password utente e la password inserita nel form di login.
        /// </summary>
        /// <param name="user">Il codice utente.</param>
        /// <param name="password">La password del form.</param>
        /// <returns></returns>
        private static bool CheckUserPasswordEquality(MembershipUser user, string password)
        {
            return user.GetPassword() == password;
        }

        private static LoginValidationResult MissingAdminProfileResponse(string userName)
        {
            LoginValidationResult result = new LoginValidationResult() { isValid = false };

            result.missingAdminUser = true;

            int userLvl = RepoManager.UtentiRepo.DbSet.Single(us => us.Codice_Utente == userName).Liv_Utente;

            if (userLvl == Common.Properties.Settings.Default.Winit_Level)
            {
                result.isWinitUser = true;
                result.validationError = "Utente con livello di amministratore mancante";
            }
            else
            {
                result.validationError = "Utente con livello di amministratore mancante. Non è possibile accedere al sistema!";
            }
            return result;

        }

        #endregion

    }
}
