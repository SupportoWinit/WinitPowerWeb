using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class Col_VarPage : BasePage, IGridPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        public IGridModule GridModule
        {
            get
            {
                return mdlCol_Var;
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

        public IQuadGridModule QuadGridModule
        {
            get
            {
                return null;
            }
        }
    }
}