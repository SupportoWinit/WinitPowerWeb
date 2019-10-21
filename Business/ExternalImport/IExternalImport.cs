using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ExternalImports
{
    public interface IExternalImport
    { 
        void GetTimbrature();

        void GetTimbrature(DateTime from, DateTime to);

        void WriteToFile();
        
    }
}
