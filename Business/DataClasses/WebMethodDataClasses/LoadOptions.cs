using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DataClasses.WebMethodDataClasses
{
    public class LoadOptions
    {
        public bool requireTotalCount;

        public bool requireGroupCount;

        public bool isCountQuery;

        public int skip;

        public int take;

        public object[] filter;

        public object[] select;

        public object[] sort;

        public object[] group;

    }
}
