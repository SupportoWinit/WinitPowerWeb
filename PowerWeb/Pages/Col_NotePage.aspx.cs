using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class Col_NotePage : BasePage, IGridPage, IPrintPage, ILogPage 
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        public IGridModule GridModule
        {
            get
            {
                return mdlCol_Note;
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
            return mdlCol_Note;
          }
        }

        public ILogModule LogModule
        {
          get
          {
            return mdlCol_Note;
          }
        }
    }
}