using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using System.Collections;
using System.Threading.Tasks;
using Domain.Extensions;
using System.Linq.Expressions;


namespace Business.Repository.Custom
{
    class Cant_VRepository : GenericRepository<Cant_V>, ICant_VRepository
    {
        public Cant_VRepository(PowerWebEntities context)
            : base(context)
        {
        }

        public override Expression<Func<Cant_V, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allFilIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id).ToList();
                    return cantv => cantv.Fil_Id == null || allFilIds.Contains(cantv.Fil_Id.Value);
                }
                else return base.Filter;
            }
        }
    }
}
