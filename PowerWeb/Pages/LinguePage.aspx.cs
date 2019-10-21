using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class LinguePage : BasePage, IGridPage, ILogPage
    {
        public IGridModule GridModule
        {
            get { return mdlLingueModule; }
        }

        public IDoubleGridModule DoubleGridModule
        {
            get { return null; }
        }

        public ITripleGridModule TripleGridModule
        {
            get { return null; }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlLingueModule;
            }
        }
    }
}