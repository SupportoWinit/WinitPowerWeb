using Business.GridHelpers.Classes.Decorations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridHelpers.Classes
{
    public class LookupColumn : Column
    {
        public LookupColumn(Type propertyType, string propertyName, string[] select) : base(propertyType, propertyName)
        {
            this.Lookup = new Lookup {
                DataSource = new DataSource {
                    Paginate = true,
                    Select = select
                }
            };
        }
    }
}
