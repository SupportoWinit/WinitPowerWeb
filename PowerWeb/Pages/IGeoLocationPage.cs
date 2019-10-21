using PowerWeb.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerWeb.Pages
{
    public interface IGeoLocationPage
    {
        IGeoLocationModule GeoLocationModule { get; }
    }
}
