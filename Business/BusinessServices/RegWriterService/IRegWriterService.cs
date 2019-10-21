using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegWriterService
{
    public interface IRegWriterService
    {
        void WriteRegs(IEnumerable<string> regs,string deviceCode);
        void WriteJsonRegsToFilesInput(IEnumerable<string> regs);
        void BackUpJsonRegs(IEnumerable<object> regs, string deviceCode);
    }
}
