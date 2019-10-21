using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using System.Collections;

namespace Business.Repository.Custom
{
  public interface ITab_GridLookupRepository : IRepository<Tab_GridLookup>
  {
      IList SearchByFieldAndValue(List<Tab_GridLookup> tabGridLookups, String field, String value, int? beginIndex = null, int? endIndex = null, IQueryable dataSource = null, bool isForReport = false);
  }
}
