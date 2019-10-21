using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class UtentiPage : BasePage, IGridPage, IPrintPage, ILogPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        public IGridModule GridModule
        {
            get
            {
                return mdlUtenti;
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

        public IPrintModule PrintModule
        {
            get
            {
                return mdlUtenti;
            }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlUtenti;
            }
        }
    }
}