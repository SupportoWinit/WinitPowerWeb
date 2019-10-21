using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Business.MDBSchema;
using System.Data.OleDb;

namespace Business.Repository.Custom
{
  public interface ITab_DecodRepository : IRepository<Tab_Decod>
  {
    Tab_Decod SearchKeyInTable(string group, string tabName, string key);

    Tab_Decod SearchDecodifyInTable(string group, string tabName, string decodify);

    IQueryable<Tab_Decod> GetAllParametrized(string group, string tabName);

    bool ExistParametrized(string group, string tabName, string key);

    List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, OleDbConnection myAccessConn, bool onlyErrors = false);
  }
}
