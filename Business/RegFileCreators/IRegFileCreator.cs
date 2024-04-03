using Business.DataClasses.FlutterAppDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.RegFileCreators
{
    public interface IRegFileCreator<T> where T : class
    {
        void WriteToFile(IEnumerable<T> unEncodedRegs, IEnumerable<FlutterAppRegOld> unEncodedRegsOld, IEnumerable<T> unEncodedRegs2);
    }
}
