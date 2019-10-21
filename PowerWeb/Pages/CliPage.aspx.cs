using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class CliPage : BasePage, IGridPage, IPrintPage, ILogPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public IGridModule GridModule
        {
            get
            {
                return mdlCli;
            }
        }

        #region IPrintPage Membri di

        public IPrintModule PrintModule
        {
            get
            {
                return mdlCli;
            }
        }

        #endregion


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
                return mdlCli;
            }
        }
    }
}