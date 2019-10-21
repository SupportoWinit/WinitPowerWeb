
using System;
using System.Collections.Generic;

namespace Exports
{
    /// <summary>
    /// Intefaccia da implementare per tutti gli export di PW.
    /// Contiene i metodi necessari per il download e le variabili per la creazione del file.
    /// </summary>
    public interface IExport
    {
        DateTime ExportDate { get; set; }
        DateTime FromDate { get; set; }
        DateTime ToDate { get; set; }

        bool IsToZip { get; set; }
        bool CentHours { get; set; }
        string FileName { get; set; }
        string Extension { get; set; }
        string[] GridFilter { get; set; }
        object[] GridColumns { get; set; }
        string ModelFilePath { get; set; }
        string DownloadPath { get; }
        int[] SelectedIds { get; set; }
        List<object> Errors { get; set; }

        /// <summary>
        /// Salva il file secondo la regola descritta all'interno dell'apposita classe astratta.
        /// </summary>
        /// <returns>Ritorna un json necessario per il download da parte del client.</returns>
        void SaveToFileSystem();

        /// <summary>
        /// Implementazione della logica di creazione del file richiesto.
        /// </summary>
        void LaunchExport();
    }
}
