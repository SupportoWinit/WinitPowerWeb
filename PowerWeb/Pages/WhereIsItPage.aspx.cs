using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class WhereIsItPage : BasePage, IGridPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        public IGridModule GridModule
        {
            get { return mdlWhereIsIt; }
        }


        public IDoubleGridModule DoubleGridModule
        {
            get { return null; }
        }

        public ITripleGridModule TripleGridModule
        {
            get { return null; }
        }
    }
}