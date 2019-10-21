using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Business.MDBSchema;
using Domain;

namespace Business.Repository.Custom
{
    public interface IUtentiRepository : IRepository<Utenti>
    {
        IQueryable<Utenti> GetAllEnabled();

        List<Utenti> GetAllExpiring(int addDays);

        Utenti GetWinitUser();
    }
}
