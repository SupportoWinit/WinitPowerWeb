using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using DevExpress.Web.ASPxGridView;
using DevExpress.XtraReports.UI;
using Domain;
using DevExpress.Web.ASPxPanel;

namespace PowerWeb.Modules
{
    public interface IPrintModule
    {
        ExtXtraReport GetReport(Tab_Report report, List<TabPageExtended> selectedTabs, Dictionary<string, int> reportOptions, List<GroupingTreeListItem> groups, List<Object> items, ASPxPanel customOptionsPanel = null);

        /// <summary>
        /// Recupera la griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// </summary>
        /// <value>
        /// La griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// </value>
        ASPxGridView PrintGridView { get; }

        /// <summary>
        /// Recupera il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </summary>
        /// <value>
        /// il template della form utilizzata per il recupero dei dati di raggruppamento in stampa;
        /// se impostato a null si utilizza il valore specificato nel modulo.
        /// </value>
        PowerFormTemplate PrintFormTemplate { get; }
    }

    public interface IPrintCustomModule : IPrintModule
    {
        ASPxPanel CustomOptionsPanel { get; }
    }
}
