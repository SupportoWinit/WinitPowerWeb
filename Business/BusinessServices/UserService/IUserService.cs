using Business.DataClasses;
using Common.Models.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.UserService
{
    public interface IUserService
    {
        LoginValidationResult Login(LoginRequest loginRequest);
    }
}
