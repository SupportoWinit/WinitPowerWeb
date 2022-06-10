using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class Reg_VPage : Page, IGridPage, IPrintPage, IExportXLSXPage, IGeoLocationPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public IGridModule GridModule
        {
            get { return mdlReg_VModule; }
        }

        public IPrintModule PrintModule
        {
            get { return mdlReg_VModule; }
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

        public IExportXLSXModule ExportXLSXModule
        {
            get { return mdlReg_VModule; }
        }

        public IGeoLocationModule GeoLocationModule
        {
            get { return mdlReg_VModule; }
        }
    }
}