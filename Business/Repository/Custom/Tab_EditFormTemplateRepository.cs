using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Linq.Expressions;
using System.Data;
using System.Data.OleDb;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Tab_EditFormTemplateRepository : GenericRepository<Tab_EditFormTemplate>, ITab_EditFormTemplateRepository
    {
        public Tab_EditFormTemplateRepository(PowerWebEntities context)
            : base(context)
        {
        }
        public override Dictionary<string, string> Check(Tab_EditFormTemplate entity, bool isNew = false, bool isResetSession = true)
        {           
            return base.Check(entity, isNew, isResetSession);
        }

        public override Tab_EditFormTemplate Init()
        {
            return base.Init();
        }

    }
}