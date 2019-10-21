using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.RegWriterService.Interfaces.Writer
{
    public interface IWriter
    {
        void Write(IEnumerable<string> regs, string device);
    }
}
