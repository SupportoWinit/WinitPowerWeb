using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class ColPage : BasePage, IGridPage, IPrintPage, ILogPage, IGeoLocationPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        public IGridModule GridModule
        {
            get
            {
                return mdlCol;
            }
        }

        public IPrintModule PrintModule
        {
            get
            {
                return mdlCol;
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

        public ILogModule LogModule
        {
            get
            {
                return mdlCol;
            }
        }

        public IGeoLocationModule GeoLocationModule
        {
            get
            {
                return mdlCol;
            }
        }
    }
}