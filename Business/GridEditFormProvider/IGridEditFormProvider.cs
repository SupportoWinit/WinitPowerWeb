using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.GridEditFormProvider
{
    public interface IGridEditFormProvider
    {
        JObject Build();
    }
}
