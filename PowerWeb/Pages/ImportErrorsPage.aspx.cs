using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class ImportErrorsPage : BasePage, IGridPage
    {
        public IGridModule GridModule
        {
            get { return mdlImportErrorsModule; }
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