using DevExpress.Web.ASPxGridView;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerWeb.Modules
{
    /// <summary>
    /// L'interfaccia utilizzata dai moduli che permettono l'esportazione dei dati di griglia su excel
    /// </summary>
    public interface IExportXLSXModule
    {
        /// <summary>
        /// Recupera l'elenco dei modelli utilizzabili in fase di export.
        /// </summary>
        /// <value>
        /// L'elenco dei modelli utilizzabili in fase di export.
        /// </value>
        List<Tab_Excel_Model> Models { get; }

        /// <summary>
        /// Recupera la griglia utilzizata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo
        /// </summary>
        /// <value>
        /// La griglia utilizzata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo.
        /// </value>
        ASPxGridView ExportGridView { get; }

        /// <summary>
        /// Effettua l'esportazione excel con i dati specificati.
        /// </summary>
        /// <param name="model">Il modello excel da utilizzare.</param>
        /// <param name="items">L'elenco degli elementi da esportare.</param>
        void ExportXLSX(Tab_Excel_Model model, List<Object> items);
    }
}
