using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.HttpHub.HttpHubs
{
    public class FlutterAppHub: FlutterAppHttpModule
    {
        public override string Host
        {
            get
            {
                return base.Host;
            }

            set
            {
                base.Host = value;
            }
        }
    }
}
