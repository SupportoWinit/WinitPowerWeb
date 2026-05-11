
using Business.BusinessExtension;
using Business.DataClasses.SupportClasses;
using Business.Repository;
using Common;
using DevExpress.XtraSpreadsheet.Model;
using Domain;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.Style;
using Spire.Additions.Xps.Schema;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Westwind.Utilities.Extensions;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportManutentori : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
    {
        #region Public Properties
        /// <summary>
        /// Recupera o imposta il percorso del modello EXCEL
        /// </summary>   
        /// <value>
        /// Percorso modello EXCEL su disco.
        /// </value>
        public string ExcelModelFilePath { get; set; }

        /// <summary>
        /// Recupera o imposta il periodo (mese/anno) di riferimento dell'export.
        /// </summary>
        /// <value>
        /// Periodo (mese/anno) di riferimento dell'export
        /// </value>
        public DateTime ExportPeriod { get; set; }

        public int rowIndex = 1;

        public int columnIndex = 1;

        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        #endregion

        #region Public Methods

        /// <summary>
        /// Esegue la preparazione dell'export con i dati passati come parametro
        /// </summary>
        /// <param name="entitiesToExport">Elenco delle entità da esportare</param>
        public override void LaunchExport(IQueryable<Reg_V> entitiesToExport)
        {
            //List<CentroDiCosto> centro = RepoManager.CentroDiCostoRepo.GetAllQueryable(c => c.Descrizione == "PULIZIE CIVILI").ToList();
            DateTime minDate = CommonService.GetFirstMonthDay(ExportPeriod);
            DateTime monthLastDate = CommonService.GetLastMonthDay(minDate);
            DateTime maxDate = new DateTime(monthLastDate.Year, monthLastDate.Month, monthLastDate.Day, 23, 59, 59);
            ExcelWorkbookGenerateNew(ExcelModelFilePath);
            Tab_Decod motivazionePausa = RepoManager.Tab_DecodRepo.Single(d => d.Chiave_Tab == "Pausa");
            var collaboratori = new List<Col>();
            List<Reg_V> exportRegs = new List<Reg_V>();
            List<Col> manutentori = RepoManager.ColRepo.GetAllQueryable(c => c.Qualifica_Col == "0").ToList();

            foreach (Col collaboratore in manutentori)
            {
                if (collaboratore.Scadenza_Patente_Col != null)
                {
                    if (collaboratore.Scadenza_Patente_Col.Value.Between(minDate, maxDate))
                    {
                        exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= collaboratore.Scadenza_Patente_Col.Value && r.Data_Reg <= maxDate && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
                        collaboratori.Add(collaboratore);
                    }
                    else
                    {
                        exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= minDate && r.Data_Reg <= maxDate && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
                        collaboratori.Add(collaboratore);
                    }
                }
                else
                {
                    exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= minDate && r.Data_Reg <= maxDate && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
                    collaboratori.Add(collaboratore);
                }
            }

            List<Col> colCambiati = RepoManager.ColRepo.GetAllQueryable(c => c.Qualifica_Col != "0" && c.Scadenza_Patente_Col.Value != null).ToList();

            foreach (Col collaboratore in colCambiati)
            {
                if (collaboratore.Scadenza_Patente_Col != null)
                {
                    if (collaboratore.Scadenza_Patente_Col.Value.Between(minDate, maxDate))
                    {
                        exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= minDate && r.Data_Reg <= collaboratore.Scadenza_Patente_Col.Value && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
                        collaboratori.Add(collaboratore);
                    }
                }
            }
            List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg >= minDate && r.Data_Reg <= maxDate && r.Qualifica_Col == "0" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList();
            //List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg > minDate && r.Data_Reg < maxDate && r.Qualifica_Col == "0").ToList();
            var exportRegVs = exportRegs.GroupBy(c => c.Cant_Id);// regVs.GroupBy(c => c.Cant_Id);
            List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));
            List<Reg_V> newRegVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Codice_Commessa_Can == "Pulizie Civile" && r.Data_Reg >= minDate && r.Data_Reg <= maxDate && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4 || r.Registrazione_Tipo_Reg == 10) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList();
            //ordino le ore in base alla ora della registrazione e le reggruppo per i cantieri
            rowIndex = 2;
            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Tipologia_Can == "ATT").ToList();
            //creo un dictionary per immagazzinare le ore, la prima key sara la descrizione del cantiere, la seconda l'attivita e l'intero il totale delle ore
            List<Dictionary<string, Dictionary<string, int>>> listaAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
            List<Dictionary<string, Dictionary<string, int>>> tmpAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
            string lastAtt = "";
            int lastAttId = 0;
            string lastCant = "";
            int lastDurata = 0;
            int totaleReg = 0;
            List<ExportColCantInt> numeroInterventFinale = new List<ExportColCantInt>();
            //vado a fare un foreach per ogni cantiere
            foreach (var reg in exportRegs.OrderBy(r => r.Col_Id).ThenBy(r => r.Data_Ora_Fis_E.ToString("yyyy-MM-dd HH:mm")).ThenBy(r => r.Data_Ora_Fis_U))
            { 
                List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                //controllo se la timbratura è un attività
                if (reg.Registrazione_Tipo_Reg == 2)
                {
                    //recupero la lista delle attività
                    List<Cant> attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id && c.Tipologia_Can == "ATT").ToList();
                    //controllo se la timbratura è associata o meno
                    if (reg.Registrazione_Stato_Reg == 1)
                    {
                        //inizializzo la variabile per controllare se ho aggiornato la lista oppure devo creare una nuova tupla
                        bool aggiornato = false;
                        //ciclo tutte le attività che ho recuperato in precedenza
                        if (listaAttivita.Count() > 0)
                        {
                            bool esiste = false;
                            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                            foreach (var att in listaAttivita)
                            {
                                if (att.First().Key == lastCant)
                                {
                                    if (!esiste) {
                                        //inizializzo un dictionary temporaneo contenente come chiave attivita e valore le ore
                                        tmpDic = att.First().Value;
                                        foreach (var lista in tmpDic)
                                        {
                                            if (lista.Key == attivita.First().Descrizione_Can)
                                            {
                                                esiste = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (esiste)
                            {
                                //recupero il totale delle ore lavorate su quel cantiere facendo una determinata attivita
                                int tmp = tmpDic[attivita.First().Descrizione_Can];
                                //incremento il totale delle ore mensili
                                tmpDic[attivita.First().Descrizione_Can] = tmp + lastDurata;
                                //azzero tute le variabili
                                lastAtt = "";
                                lastAttId = 0;
                                lastDurata = 0;
                                aggiornato = true;
                            }
                            else
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                    tmpDi[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpDi);
                                }
                                aggiornato = true;
                                lastAtt = "";
                                lastAttId = 0;
                                lastDurata = 0;
                            }
                            if (!aggiornato) {
                                if (lastDurata > 0) {
                                    var tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                    tmpdic[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpdic);
                                }
                                lastAtt = "";
                                lastAttId = 0;
                            }
                        }
                        else {
                            if (lastDurata > 0) {
                                Dictionary<string, Dictionary<string, int>> tmpDic = new Dictionary<string, Dictionary<string, int>>();
                                tmpDic[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                tmpAttivita.Add(tmpDic);
                            }
                            lastDurata = 0;
                            lastAtt = "";
                            lastAttId = 0;
                        }

                    }
                    else {
                        if (lastAtt != "") {
                            attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == lastAttId && c.Tipologia_Can == "ATT").ToList();
                            //inizializzo la variabile per controllare se ho aggiornato la lista oppure devo creare una nuova tupla
                            bool aggiornato = false;
                            bool esiste = false;
                            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                            foreach (var att in listaAttivita)
                            {
                                if (att.First().Key == lastCant)
                                {
                                    if (!esiste)
                                    {
                                        //inizializzo un dictionary temporaneo contenente come chiave attivita e valore le ore
                                        tmpDic = att.First().Value;
                                        foreach (var lista in tmpDic)
                                        {
                                            if (lista.Key == attivita.First().Descrizione_Can)
                                            {
                                                esiste = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (esiste)
                            {
                                //recupero il totale delle ore lavorate su quel cantiere facendo una determinata attivita
                                int tmp = tmpDic[attivita.First().Descrizione_Can];
                                //incremento il totale delle ore mensili
                                tmpDic[attivita.First().Descrizione_Can] = tmp + lastDurata;
                                //azzero tute le variabili
                                lastAtt = "";
                                lastAttId = 0;
                                lastDurata = 0;
                                aggiornato = true;
                            }
                            else
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                    tmpDi[lastCant] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpDi);
                                }
                                aggiornato = true;
                                lastAtt = "";
                                lastAttId = 0;
                                lastDurata = 0;
                            }
                            if (!aggiornato)
                            {
                                if (lastDurata > 0) {
                                    Dictionary<string, Dictionary<string, int>> tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                    tmpdic[currentCant.First().Descrizione_Can] = new Dictionary<string, int>() { { attivita.First().Descrizione_Can, lastDurata } };
                                    tmpAttivita.Add(tmpdic);
                                }
                                lastAtt = "";
                                lastAttId = 0;
                                lastDurata = 0;
                            }
                        }
                        attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                        lastAtt = attivita.First().Descrizione_Can;
                        lastAttId = attivita.First().Cant_Id;
                    }
                } 
                else if (reg.Registrazione_Tipo_Reg == 0) {
                    if (lastAtt != "" && lastCant != reg.Cant_Desc)
                    {
                        //recupero la lista delle attività
                        List<Cant> attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == lastAttId).ToList();
                        //inizializzo la variabile per controllare se ho aggiornato la lista oppure devo creare una nuova tupla
                        bool aggiornato = false;
                        //ciclo tutte le attività che ho recuperato in precedenza
                        if (listaAttivita.Count() > 0)
                        {
                            bool esiste = false;
                            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                            foreach (var att in listaAttivita)
                            {
                                if (att.First().Key == lastCant)
                                {
                                    if (!esiste)
                                    {
                                        //inizializzo un dictionary temporaneo contenente come chiave attivita e valore le ore
                                        tmpDic = att.First().Value;
                                        foreach (var lista in tmpDic)
                                        {
                                            if (lista.Key == attivita.First().Note_Can)
                                            {
                                                esiste = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (esiste)
                            {
                                //recupero il totale delle ore lavorate su quel cantiere facendo una determinata attivita
                                int tmp = tmpDic[attivita.First().Note_Can];
                                //incremento il totale delle ore mensili
                                tmpDic[attivita.First().Note_Can] = tmp + lastDurata;
                                //azzero tute le variabili
                                lastAtt = attivita.First().Descrizione_Can;
                                lastAttId = attivita.First().Cant_Id;
                                lastDurata = 0;
                                aggiornato = true;
                            }
                            else
                            {
                                if (lastDurata > 0)
                                {
                                    Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                    tmpDi[lastCant] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                    tmpAttivita.Add(tmpDi);
                                }
                                aggiornato = true;
                                lastAtt = attivita.First().Descrizione_Can;
                                lastAttId = attivita.First().Cant_Id;
                                lastDurata = 0;
                            }
                            if (!aggiornato)
                            {
                                if (lastDurata > 0)
                                {
                                    var tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                    tmpdic[lastCant] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                    tmpAttivita.Add(tmpdic);
                                }
                                lastAtt = attivita.First().Descrizione_Can;
                                lastAttId = attivita.First().Cant_Id;
                            }
                        }
                        else
                        {
                            if (lastDurata > 0)
                            {
                                Dictionary<string, Dictionary<string, int>> tmpDic = new Dictionary<string, Dictionary<string, int>>();
                                tmpDic[lastCant] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                tmpAttivita.Add(tmpDic);
                            }
                            lastDurata = 0;
                            lastAtt = attivita.First().Descrizione_Can;
                            lastAttId = attivita.First().Cant_Id;
                        }
                    }
                    if (reg.Durata_Fig != null) {
                        lastDurata += reg.Durata_Fig.Value;
                        lastCant = reg.Cant_Desc;
                    }
                }
                else if (reg.Registrazione_Tipo_Reg == 4)
                {
                    if (reg.Durata_Fig != null)
                    {
                        lastDurata += reg.Durata_Fig.Value;
                        lastCant = reg.Cant_Desc;
                    }
                }
                listaAttivita = tmpAttivita;
            }

            foreach (var regs in newRegVs.GroupBy(c => c.Cant_Id)) 
            { 
                Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regs.Key).FirstOrDefault();
                if (cantiere != default(Cant)) 
                {
                    int durata = 0;
                    foreach (var reg in regs)
                    {
                        if (cantiere.Metodo_Arrotondamento_Can == 0 || cantiere.Metodo_Arrotondamento_Can == 2)
                        {
                            if (reg.Durata_Fig != null)
                                durata += reg.Durata_Fig.Value;
                        }
                        else 
                        {
                            if (reg.Durata_Fis != null)
                                durata += reg.Durata_Fis.Value;
                        }
                    }
                    Dictionary<string, Dictionary<string, int>> tmpDic = new Dictionary<string, Dictionary<string, int>>();
                    Cli cliente = RepoManager.CliRepo.GetAllQueryable(c => c.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                    if (cantiere.Tipo_Interv_Can != null) 
                    {
                        string tipoInt = cantiere.Tipo_Interv_Can;
                        Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                        if (att != null) 
                        {
                            if (cliente != default(Cli))
                            {
                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == cliente.Cognome_Cli && ex.descrizioneCol == att.Campo1_Tab);
                                if (exOgg != null)
                                {
                                    exOgg.durata += durata;
                                }
                                else 
                                {
                                    ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, att.Campo1_Tab, att.Decodifica_Tab, durata, 0);
                                    numeroInterventFinale.Add(newOgg);
                                }  
                            }
                            else 
                            {
                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == att.Campo1_Tab);
                                if (exOgg != null)
                                {
                                    exOgg.durata += durata;
                                }
                                else 
                                {
                                    ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, att.Campo1_Tab, att.Decodifica_Tab, durata, 0);
                                    numeroInterventFinale.Add(newOgg);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var prova in listaAttivita.OrderBy(p => p.First().Key)) {
                CellInsertValue(1, 1, rowIndex, prova.First().Key + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                foreach (var inner in prova.First().Value) {
                    List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == inner.Key && c.Tipologia_Can == "ATT").ToList();

                    CellInsertValue(1, 2, rowIndex, cants.Last().Note_Can + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                    RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);

                    CellInsertValue(1, 3, rowIndex, inner.Key + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);
                    RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);

                    TimeSpan durata = TimeSpan.FromMinutes(inner.Value);
                    int minuti = 00;
                    switch (durata.Minutes) {
                        case 15:
                            minuti = 25;
                            break;
                        case 30:
                            minuti = 50;
                            break;     
                        case 45:
                            minuti = 75;
                            break;
                        }
                    string totale = String.Format("{0},{1}", (durata.Days * 24) + durata.Hours, minuti.ToString("00"));
                    CellInsertValue(1, 4, rowIndex, totale + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);
                    RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);
                }

                
                rowIndex++;
            }

            foreach (var prova in numeroInterventFinale.OrderBy(ni => ni.descrizioneCant))
            {
                List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == prova.descrizioneCant).ToList();
                string descrizioneCliente = "";
                if (cants.First().Cli_Id != null) 
                { 
                    int cliId = cants.First().Cli_Id.Value;
                    Cli cliente = RepoManager.CliRepo.GetAllQueryable(c => c.Cli_Id == cliId).FirstOrDefault();
                    if (cliente != default(Cli))
                    {
                        descrizioneCliente = cliente.Cognome_Cli;
                    }
                }

                CellInsertValue(1, 1, rowIndex, descrizioneCliente, ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                CellInsertValue(1, 2, rowIndex, prova.descrizioneCant, ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);

                CellInsertValue(1, 3, rowIndex, prova.descrizioneCol, ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);
                RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);

                CellInsertValue(1, 4, rowIndex, prova.descrizioneAtt, ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);
                RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);

                TimeSpan durata = TimeSpan.FromMinutes(prova.durata);
                int minuti = 00;
                switch (durata.Minutes)
                {
                    case 15:
                        minuti = 25;
                        break;
                    case 30:
                        minuti = 50;
                        break;
                    case 45:
                        minuti = 75;
                        break;
                }
                string totale = String.Format("{0},{1}", (durata.Days * 24) + durata.Hours, minuti.ToString("00"));
                CellInsertValue(1, 5, rowIndex, totale + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 5, rowIndex, 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 5, rowIndex, 5, rowIndex, 11);
                RangeSetWrapText(1, 5, rowIndex, 5, rowIndex, true);
                


                rowIndex++;
            }

            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            ExcelWorkbookDispose();
        }

        /// <summary>
        /// Metodo utilizzato dalle classi figlie come porta d'ingresso principale per il lancio dell'export.
        /// </summary>
        /// <param name="selectedColIds">L'elenco degli id collaboratore selezionati per l'export.</param>
        /// <param name="selectedCantIds">L'elenco degli id cantiere selezionati per l'export.</param>
        /// <param name="selectedCliIds">L'elenco degli id cliente selezionati per l'export.</param>
        public override void LaunchExport(IEnumerable<int> selectedColIds, IEnumerable<int> selectedCantIds, IEnumerable<int> selectedCliIds)
        {
            //    if (selectedCliIds.Any())
            //    {
            //        DateTime firstMonthDate = CommonService.GetFirstMonthDay(ExportPeriod);
            //        DateTime lastMonthDate = CommonService.GetLastMonthDay(ExportPeriod);
            //        Col col;

            //        ExcelWorkbookGenerateNew(ExcelModelFilePath);

            //        foreach (int cliId in selectedCliIds)
            //        {
            //            foreach (int colid in selectedColIds)
            //            {
            //                IEnumerable<Reg_V> colRegVs = GetProcessableColRegVs(colid, firstMonthDate, lastMonthDate); // modificare con ID collaboratore (where the fuck i can find it?)

            //                if (colRegVs.Any())
            //                {
            //                    col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colid);

            //                }
            //            }
            //        }
            //    }
            throw new NotImplementedException();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Recupera tutte le registrazioni processabili del collaboratore X per il periodo specificato.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore per cui effettuare la ricerca.</param>
        /// <param name="startPeriod">La data di inizio del periodo in cui effettuare la ricerca.</param>
        /// <param name="endPeriod">La data di fine del periodo in cui effettuare la ricerca.</param>
        /// <returns>L'elenco delle registrazioni da processare per il collaboratore e il periodo scelto.</returns>
        private IEnumerable<Reg_V> GetProcessableColRegVs(int colId, DateTime startPeriod, DateTime endPeriod)
        {
            return RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == colId && regv.Data_Reg >= startPeriod && regv.Data_Reg <= endPeriod);
        }

        #endregion
    }
}
