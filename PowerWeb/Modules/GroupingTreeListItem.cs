using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DevExpress.Web.ASPxClasses;

namespace PowerWeb.Modules
{
    public class GroupingTreeListItems : List<GroupingTreeListItem> { }

    public class GroupingTreeListItem
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public String Caption { get; set; }
        public String Field { get; set; }
        public bool IsSkipPage { get; set; }
        public bool RepeatEveryPage { get; set; }
        public bool IsInGrouping { get; set; }
        
    }
}