using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class TimesheetPage : BasePage, IGridPage, IPrintPage, IExportXLSXPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }


        public IGridModule GridModule
        {
            get { return mdlTimesheetModule; }
        }

        public IDoubleGridModule DoubleGridModule
        {
            get { return mdlTimesheetModule; }
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
            get { return mdlTimesheetModule; }
        }

        public IExportXLSXModule ExportXLSXModule
        {
            get { return mdlTimesheetModule; }
        }
    }
}