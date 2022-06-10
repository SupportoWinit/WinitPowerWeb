using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class PruPage : BasePage, IGridPage, IPrintPage, ILogPage
    {
        public IGridModule GridModule
        {
            get
            {
                return mdlPru;
            }
        }

        public IDoubleGridModule DoubleGridModule
        {
            get { return null; }
        }

        public ITripleGridModule TripleGridModule
        {
            get { return null; }
        }

        public IQuadGridModule QuadGridModule
        {
            get { return null; }
        }

        public IPrintModule PrintModule
        {
            get
            {
                return mdlPru;
            }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlPru;
            }
        }
    }
}