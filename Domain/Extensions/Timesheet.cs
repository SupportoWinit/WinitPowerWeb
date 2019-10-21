using System;
using Common;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Timesheet
    {
        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(Timesheet);
                PropertyInfo propInfo = myType.GetProperty(propertyName);
                return propInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(Timesheet);
                PropertyInfo propInfo = myType.GetProperty(propertyName);
                propInfo.SetValue(this, value, null);
            }
        }
    }
}
