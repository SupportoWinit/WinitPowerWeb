using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Business.MDBSchema;
using System.Data;

namespace Business.Repository.Custom
{
  public interface ITab_Chk_ImpRepository : IRepository<Tab_Chk_Imp>
  {
    List<T> GetImportErrorData<T>(List<T> dataList, string tableName, string keyField) where T : DataRow;
  }
}
