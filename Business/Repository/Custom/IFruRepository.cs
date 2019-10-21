using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
  public interface IFruRepository : IRepository<Fru>
  {
      Dictionary<string, string> ImportFromTXT(String[] inputFile);

      List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet dataSet, String tableName, bool onlyErrors = false);
  }
}
