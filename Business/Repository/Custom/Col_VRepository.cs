using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Collections;
using Business.MDBSchema;
using System.Linq.Expressions;
using log4net;

namespace Business.Repository.Custom
{
    public class Col_VRepository : GenericRepository<Col_V>, ICol_VRepository
    {
        public Col_VRepository(PowerWebEntities context)
            : base(context)
        {
        }

        public override Expression<Func<Col_V, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10) 
                {
                    var allRespIds = PowerWebContext.Current.Resps.Select(resp => resp.Resp_Id).ToList();
                    return colv => colv.Resp_Id == null || allRespIds.Contains(colv.Resp_Id.Value);
                }
                else return base.Filter;
            }
        }
    }
}


