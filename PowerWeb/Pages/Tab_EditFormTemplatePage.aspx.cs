using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class Tab_EditFormTemplatePage : BasePage, IGridPage, ILogPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        public IGridModule GridModule
        {
            get { return mdlTab_EditFormTemplate; }
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


        public ILogModule LogModule
        {
            get { return mdlTab_EditFormTemplate; }
        }

    }
}