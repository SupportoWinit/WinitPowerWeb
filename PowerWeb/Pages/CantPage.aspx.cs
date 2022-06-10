using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class CantPage : BasePage, IGridPage, IPrintPage, ILogPage, IGeoLocationPage, IExportXLSXPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public IGridModule GridModule
        {
            get { return mdlCant; }
        }

        public IPrintModule PrintModule
        {
            get { return mdlCant; }
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
            get
            {
                return null;
            }
        }
        public ILogModule LogModule
        {
            get { return mdlCant; }
        }

        public IGeoLocationModule GeoLocationModule
        {
            get { return mdlCant; }
        }

        public IExportXLSXModule ExportXLSXModule
        {
            get { return mdlCant; }
        }}
}