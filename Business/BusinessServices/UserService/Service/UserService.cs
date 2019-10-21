using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DataClasses;
using Common.Models.Login;

namespace Business.BusinessServices.UserService.Service
{
    public class UserService : IUserService
    {
        public LoginValidationResult Login(LoginRequest loginRequest)
        {
            LoginValidationResult result = new LoginValidationResult();

            bool isValid = BusinessService.ValidateUser(loginRequest.UserName, loginRequest.Password, result);

            if (loginRequest.DoubleCheck)
            {
                if (!result.isDoubleLoginAllowed)
                {
                    result.AddFormErrors("userName", "Attenzione,l'utente non è abilitato per il login con supervisione!");
                    result.isValid = false;
                    return result;
                }
                   
                isValid = BusinessService.ValidateSuperUser(loginRequest.SuperUserName, loginRequest.SuperUserPassword, result);
            }
            if (isValid)
                BusinessService.StartUserSession(loginRequest.UserName, loginRequest.SuperUserName);

            return result;
        }
    }
}
