using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes
{
    public class ForeignKeyColumn : Column
    {
        public ForeignKeyColumn(Type propertyType, string propertyName) : base(propertyType, propertyName)
        {
            this.DataType = "object";
        }
    }
}
