using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DevExpress.Web.ASPxGridView;
using System.Collections;
using DevExpress.Data.Filtering;

namespace PowerWeb.Modules
{
    public interface IGridModule
    {
        ASPxGridView GridView { get; }

        ASPxGridView GridViewDetail { get; }

        PowerFormTemplate EditFormTemplate { get; }

        PowerFormTemplate EditDetailFormTemplate { get; }

        Boolean IsToPopulateGrid { get; set; }

        Type EntityType { get; }

        Type DetailGridEntityType { get; }

        void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e);

        CriteriaOperator DefaultFilter { get; }
    }

    public interface IDoubleGridModule : IGridModule
    {
        ASPxGridView GridView2 { get; }

        PowerFormTemplate EditFormTemplate2 { get; }
    }

    public interface ITripleGridModule : IDoubleGridModule
    {
        ASPxGridView GridView3 { get; }

        PowerFormTemplate EditFormTemplate3 { get; }
    }

    public interface IQuadGridModule : ITripleGridModule
    {
        ASPxGridView GridView4 { get; }
        PowerFormTemplate EditFormTemplate4 { get; }
    }
}