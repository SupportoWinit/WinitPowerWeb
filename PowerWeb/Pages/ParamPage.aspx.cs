using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class ParamPage : BasePage, IGridPage, IPrintPage, ILogPage, IExportXLSXPage
    {
        public IGridModule GridModule
        {
            get { return mdlParamModule; }
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
            get { return mdlParamModule; }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlParamModule;
            }
        }

        public IExportXLSXModule ExportXLSXModule
        {
            get
            {
                return mdlParamModule;
            }
        }
    }
}