using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DevExpress.Web.ASPxGridView;
using PowerWeb.Modules;

namespace PowerWeb.Pages
{
    public interface IGridPage
    {
        IGridModule GridModule { get; }
        IDoubleGridModule DoubleGridModule { get; }
        ITripleGridModule TripleGridModule { get; }
    }
}