using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class RespPage : BasePage, IGridPage, IPrintPage, ILogPage
    {
        public IGridModule GridModule
        {
            get { return mdlRespModule; }
        }

        public IDoubleGridModule DoubleGridModule
        {
            get { return null; }
        }

        public ITripleGridModule TripleGridModule
        {
            get { return null; }
        }
        
        public IPrintModule PrintModule
        {
            get
            {
                return mdlRespModule;
            }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlRespModule;
            }
        }
    }
}