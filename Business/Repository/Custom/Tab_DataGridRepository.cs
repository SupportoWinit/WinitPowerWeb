using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;

namespace Business.Repository.Custom
{
    public class Tab_DataGridRepository : GenericRepository<Tab_DataGrid>, ITab_DataGridRepository
    {
        public Tab_DataGridRepository(PowerWebEntities context)
            : base(context)
        {

        }

        public override Dictionary<string, string> Check(Tab_DataGrid entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            WriteCheckLog(entity, result, Log);
            return result;
        }
    }
}
