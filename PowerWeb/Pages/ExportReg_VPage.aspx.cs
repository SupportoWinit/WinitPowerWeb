using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class ExportReg_VPage : Page, IGridPage
    {

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public IGridModule GridModule
        {
            get
            {
                return mdlExportReg_VModule;
            }
            
        }

        public IDoubleGridModule DoubleGridModule
        {
            get
            {
                return mdlExportReg_VModule;
            }
            
        }

        public ITripleGridModule TripleGridModule 
        { 
            get
            {
                return null;
            }
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