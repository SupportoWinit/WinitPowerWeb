using System;
using System.Collections.Generic;
using System.Linq;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public partial class SchedulesPage : BasePage, IGridPage, ILogPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public IGridModule GridModule
        {
            get
            {
                return mdlSchedules;
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

        public ILogModule LogModule
        {
            get
            {
                return mdlSchedules;
            }
        }
    }
}