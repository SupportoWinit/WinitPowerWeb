using Business.DataClasses;
using Business.Profile;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PowerWeb.Pages.Account
{
    public partial class ChangePasswordPage : System.Web.UI.Page
    {
        public static string user;
        public string secretQuestion;
        public static string secretAnswer;

        protected void Page_Init(object sender,EventArgs e)
        {
            var dict = HttpUtility.ParseQueryString(((Page)sender).ClientQueryString);
            var json = new JavaScriptSerializer().Serialize(dict.AllKeys.ToDictionary(k => k, k => dict[k]));
            JObject requestPayLoad = JObject.Parse(json);

            user = requestPayLoad["user"].ToString();

            secretQuestion = PowerWebMembershipProvider.GetUserSecretQuestion(user);
            secretAnswer = PowerWebMembershipProvider.GetUserSecretAnswer(user);


        }
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string ValidateQuestion(string answer)
        {
            return (answer == secretAnswer).ToString().ToLower();
        }

        [WebMethod]
        public static string ValidatePassword(string password)
        {

            LoginValidationResult result = PowerWebMembershipProvider.ValidatePassword(user, password, false);

            if (result.isValid)
            {
                string error = "";

                PowerWebMembershipProvider.UpdatePassWord(user, password, ref error);
            } 

            return JsonConvert.SerializeObject(result);
        }

    }
}