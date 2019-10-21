using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes
{
    public class KeyColumn : Column
    {
        public KeyColumn(Type propertyType, string propertyName) : base(propertyType, propertyName)
        {
            this.ShowInColumnChooser = false;
        }
    }
}
