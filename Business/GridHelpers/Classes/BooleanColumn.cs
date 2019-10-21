using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes
{
    public class BooleanColumn : Column
    {
        public BooleanColumn(Type propertyType, string propertyName) : base(propertyType, propertyName)
        {
        }
    }
}
