using System;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class MonteMinutiPage : BasePage, IGridPage
    {
        private const string CustomExportNamespace = "Exports";

        protected void Page_Load(object sender, EventArgs e)
        {
        }
        
        public IGridModule GridModule
        {
            get { return (IGridModule)mdlMonteMinuti; }
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
    }
}