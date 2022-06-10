using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;
using Business.Repository;
using Common;
using DevExpress.Web.ASPxEditors;
using Domain;

namespace PowerWeb.Pages
{
    public partial class Reg_VMPage : Page, IGridPage, IPrintPage, IExportXLSXPage, IGeoLocationPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            
        }

        public IGridModule GridModule
        {
            get { return mdlReg_VMModule; }
        }

        public IPrintModule PrintModule
        {
            get { return mdlReg_VMModule; }
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
            get { return mdlReg_VMModule; }
        }

        public IGeoLocationModule GeoLocationModule
        {
            get { return mdlReg_VMModule; }
        }
    }
}