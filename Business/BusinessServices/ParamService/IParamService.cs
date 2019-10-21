using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ParamService
{
    public interface IParamService
    {
        void UpdateBlockDate(DateTime newBlockDate);

        Param Load();
    }
}
