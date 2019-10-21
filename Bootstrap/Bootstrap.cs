using Business.BusinessServices.UserService;
using Business.BusinessServices.UserService.Service;
using Business.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Bootstrap
{
    public static class Bootstrap
    {

        public static void InitializeContainer(IUnityContainer container)
        {
            container.RegisterType<IUserService, UserService>();
        }

    }
}
