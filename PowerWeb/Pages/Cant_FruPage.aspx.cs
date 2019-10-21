using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class Cant_FruPage : BasePage, IGridPage, IPrintPage, ILogPage
    {
        public IGridModule GridModule
        {
            get { return mdlCant_FruModule; }
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
            get { return mdlCant_FruModule; }
        }

        public ILogModule LogModule
        {
            get
            {
                return mdlCant_FruModule;
            }
        }
    }
}