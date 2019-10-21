using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Business.MDBSchema;
using Common;
using System.Text;
using System.Data;

namespace Business.Repository.Custom
{
  public class Tab_Chk_ImpRepository : GenericRepository<Tab_Chk_Imp>, ITab_Chk_ImpRepository
  {
    public Tab_Chk_ImpRepository(PowerWebEntities context)
      : base(context)
    {
    }

    public List<T> GetImportErrorData<T>(List<T> dataList, string tableName, string keyField) where T : DataRow
    {
      var errors = RepoManager.Tab_Chk_ImpRepo.Find(tb => tb.Nome_Tabella_Tab_Check_Imp == tableName);
      var keys = errors.Select(tb => tb.Chiave_Record_Tab_Check_Imp);
      // Cancella i precedenti errori relativi alla Tabella che si sta importando
      RepoManager.Tab_Chk_ImpRepo.Delete(errors);
      List<T> filteredData = dataList;
      if (keys.Count() > 0)
        filteredData = dataList.FindAll(x => keys.Contains(x[keyField].ToString()));
      return filteredData;
    }
  }
}
