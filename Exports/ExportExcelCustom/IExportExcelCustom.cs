using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Exports.ExportExcelCustom
{
    /// <summary>
    /// Interfaccia che rende comuni i metodi per processare nel modulo gli export custom
    /// </summary>
    public interface IExportExcelCustom<T> where T : class

    {
        /// <summary>
        /// Esege la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">L'elenco delle entità da esportare</param>
        void LaunchExport(IQueryable<T> entitiesToExport);

        /// <summary>
        /// Esegue la preparazione dell'export con i dati passati come parametro.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds);

        /// <summary>
        /// Salva l'excel in un determinato percorso  su disco.
        /// </summary>
        /// <value>
        /// Il percorso dell' excel su disco.
        /// </value>
        void ExcelWorkbookSaveToDisk(string path);
        /// <summary>
        /// Recupera o imposta il percorso del modello excel su disco.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        /// 

        /// <summary>
        /// Salva il memorystream dell'export nella session.
        /// </summary>
        /// <value>
        /// Il percorso del modello excel su disco.
        /// </value>
        /// 
        string ExcelWorkbookSaveToSession(string fileName);

        string ExcelModelFilePath { get; set; }

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Il periodo (mese/anno) di riferimento dell'export.
        /// </value>
        DateTime ExportPeriod { get; set; }

        /// <summary>
        /// Rappresenta il filtro della griglia corrente
        /// </summary>
        string[] GridFilter { get; set; }
        

        /// <summary>
        /// Percorso di salvataggio del file sul server
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario impostare uno specifico calcolo (figurative/fisiche).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario impostare uno specifico calcolo (figurative/fisiche); altrimenti, <c>false</c>.
        /// </value>
        bool UseCalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che indica se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita).
        /// </summary>
        /// <value>
        /// <c>true</c> se è necessario utilizzare un calcolo specifico di ore (solo durata/con entrata uscita); altrimenti, <c>false</c>.
        /// </value>
        bool UseHourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore che determina se utilizzare o meno la tolleranza della durata registrazione in fase di elaborazione.
        /// </summary>
        /// <value>
        /// <c>true</c> se si utilizzerà la tolleranza della durata registrazione in fase di elaborazione; altrimenti, <c>false</c>.
        /// </value>
        bool UseDurationTollerance { get; set; }


        /// <summary>
        /// Recupera o imposta un valore ch indica quando utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare in fase di elaborazione la tolleranza sui valori di entrata/uscita; altrimenti, <c>false</c>.
        /// </value>
        bool UseEUTollerance { get; set; }
        /// <summary>
        /// Recupera o imposta un valore ch indica se utilizzare oppure no l'export del confronto ore budget dettagliato
        /// </summary>
        /// <value>
        /// <c>true</c> se si deve utilizzare oppure no l'export dettagliato; altrimenti, <c>false</c>.
        /// </value>
        bool UseExportDetail { get; set; }


        /// <summary>
        /// Recupera o imposta lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Lo specifico calcolo da utilizzare (figurative/fisiche); utilizato solo se <see cref="UseCalculationType"/> è valorizzato a <c>true</c>.
        /// </value>
        ExportRegVCalculationTypeEnum CalculationType { get; set; }

        /// <summary>
        /// Recupera o imposta il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il tipo di calcolo specifico delle ore (solo durata/con entrata uscita); utilizzato solo se <see cref="UseHourType"/> è valorizzato a <c>true</c>.
        /// </value>
        ExportRegVHourTypeEnum HourType { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance"/> è valorizzato
        /// a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sulla durata utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseDurationTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        int DurationTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </summary>
        /// <value>
        /// Il valore in minuti della tolleranza sull'entrata/uscita utilizzata in fase di elaborazione; valore utilizzato solo se <see cref="UseEUTollerance"/> è valorizzato a <c>true</c>.
        /// </value>
        int EUTollerance { get; set; }

        /// <summary>
        /// Recupera o imposta la stringa che rappresenta l'entità di primo riferimento per selezione del modello excel.
        /// </summary>
        /// <value>
        /// La stringa che rappresenta l'entità di primo riferimento per la selezione del modello excel.
        /// </value>
        ExcelModelSelectionTypeEnum ModelFirstEntity { get; set; }

        /// <summary>
        /// Rappresenta la necessità di comprimere.
        /// </summary>
        /// <value>
        /// La stringa che rappresenta l'entità di primo riferimento per la selezione del modello excel.
        /// </value>
        bool Compress { get; set; }

    }
}
