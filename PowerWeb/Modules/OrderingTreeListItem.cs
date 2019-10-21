using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PowerWeb.Modules
{
    public class OrderingTreeListItems : List<OrderingTreeListItem> { }

    public class OrderingTreeListItem
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public String Caption { get; set; }
        public String Field { get; set; }
        public bool IsInOrdering { get; set; }
    }
}