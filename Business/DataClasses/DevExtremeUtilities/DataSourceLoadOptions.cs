using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExtreme.AspNet.Data;
using Business.DataClasses.WebMethodDataClasses;
using System.Collections;

namespace Business.DataClasses.DevExtremeUtilities
{
    public class DataSourceLoadOptions : DataSourceLoadOptionsBase
    {
        public static void ParseOptions(DataSourceLoadOptionsBase dataOptions, LoadOptions options)
        {
            dataOptions.IsCountQuery = options.isCountQuery;
            dataOptions.Skip = options.skip;
            dataOptions.Take = options.take;
            dataOptions.RequireTotalCount = options.requireTotalCount;
            dataOptions.RequireGroupCount = options.requireGroupCount;
            dataOptions.Select = options.select != null ? options.select.Cast<string>().ToArray() : new string[] { };
            dataOptions.Group = options.group != null ? options.group.Select(g => {
                return Newtonsoft.Json.Linq.JObject.FromObject(g).ToObject<GroupingInfo>();
            }).ToArray() : new GroupingInfo[] { };
            dataOptions.Sort = options.sort != null ? options.sort.Select(g => {
                return Newtonsoft.Json.Linq.JObject.FromObject(g).ToObject<SortingInfo>();
            }).ToArray() : new SortingInfo[] { };

            if (options.filter != null)
            {
                dataOptions.Filter = options.filter as IList;
            }
        }
    }
}
