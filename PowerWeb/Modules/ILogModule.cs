using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace PowerWeb.Modules
{
    public interface ILogModule
    {
        ILog Log { get; }
    }
}
