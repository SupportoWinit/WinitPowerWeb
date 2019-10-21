
using Business.Profile;
using Business.Repository;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PowerWeb.Api
{
    public class CheckPasswordExpirationController : GenericApi
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CheckPasswordExpirationController));

        protected override void ExecuteOperation()
        {

            List<Utenti> expiredUsers = new List<Utenti>();

            List<Utenti> allUsers = RepoManager.UtentiRepo.GetAll(true).ToList();

            allUsers.ForEach(user => {

                if (!PowerWebMembershipProvider.CheckUserPassWordExpiration(user.Codice_Utente, false).isValid)
                {
                    expiredUsers.Add(user);
                }

            });

            List<Utenti> expiringUsers = RepoManager.UtentiRepo.GetAllExpiring(30);

            if (expiredUsers.Any() || expiringUsers.Any())
            {
                Business.BusinessService.SendExpiredUsersEmail(expiredUsers, expiringUsers);
            }
        }
    }
}