
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
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /// <summary>
    /// Classe utilizzata per la gestione dell'export per fatture
    /// </summary>
    public class ExportAnalitica : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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
        private OfficeOpenXml.Style.ExcelBorderStyle borderStyleThick = OfficeOpenXml.Style.ExcelBorderStyle.Thick;
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
            ExcelWorkbookGenerateNew(ExcelModelFilePath);
            rowIndex = 2;
            List<CentroDiCosto> cdc = RepoManager.CentroDiCostoRepo.GetAll().ToList();
            List<Reg_V> regVs = new List<Reg_V>();

            DateTime startMonth = CommonService.GetFirstMonthDay(ExportPeriod);
            DateTime endMonth = CommonService.GetLastMonthDay(ExportPeriod);

            Tab_Decod motivazionePausa = RepoManager.Tab_DecodRepo.Single(d => d.Chiave_Tab == "Pausa");
            List<ExportColCantDur> dataToExport = new List<ExportColCantDur>();
            var collaboratori = new List<Col>();
            List<Reg_V> exportRegs = new List<Reg_V>();
            List<Reg_V> exportRegsHotel = new List<Reg_V>();
            Cli cliente = RepoManager.CliRepo.FirstOrDefault(cl => cl.Cognome_Cli == "COMUNE LIMONE");
            int cliId = cliente.Cli_Id;
            //List<Reg_V> regVs2 = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && r.Qualifica_Col == "0" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList();
            exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Codice_Commessa_Can != "Hotel" && r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4 || r.Registrazione_Tipo_Reg == 10) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            exportRegsHotel.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Codice_Commessa_Can == "Hotel" && r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4 || r.Registrazione_Tipo_Reg == 8 || r.Registrazione_Tipo_Reg == 10)).ToList());
            var exportRegVsHotel = exportRegsHotel.GroupBy(c => c.Col_Id);
            var exportRegVsLimone = exportRegs.Where(r => r.CentroDiCosto_Id == 6 && r.Registrazione_Tipo_Reg == 0);
            exportRegs = exportRegs.Where(r => r.CentroDiCosto_Id != 6).ToList();
            var exportRegVs = exportRegs.GroupBy(c => c.Col_Id);
            collaboratori = new List<Col>();
            foreach (var exportReg in exportRegVs)
            {
                Col collaboratore = RepoManager.ColRepo.FirstOrDefault(c => c.Col_Id == exportReg.Key);
                collaboratori.Add(collaboratore);
                for (DateTime cond = startMonth; cond.Month <= endMonth.Month && cond.Year == endMonth.Year; cond = cond.AddDays(1))
                {
                    //creo un dictionary per immagazzinare le ore, la prima key sara la descrizione del cantiere, la seconda l'attivita e l'intero il totale delle ore
                    List<Dictionary<string, Dictionary<string, int>>> listaAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
                    //vado a fare un foreach
                    var list = exportReg.Where(r => r.Data_Reg.Value == cond);
                    foreach (var reg in list)
                    {
                        if (reg.Cant_Id != null && reg.Durata_Fig != null)
                        {
                            List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                            if (cants.First().Tipo_Interv_Can != null)
                            {
                                string tipoInt = cants.First().Tipo_Interv_Can;
                                Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                if (att != default(Tab_Decod))
                                {
                                    if (cants.First().Descrizione_Can.Contains("COMUNE LIMONE") && cants.First().Descrizione_Can.Contains("CENTRO ASSISTENZIALE"))
                                    {
                                        if (dataToExport.Count == 0)
                                        {
                                            ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab,collaboratore.Cognome_Col,collaboratore.Nome_Col,reg.Durata_Fig.Value);
                                            dataToExport.Add(oggToAdd);
                                        }
                                        else 
                                        {
                                            ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                            if (oggToFind != default(ExportColCantDur))
                                            {
                                                oggToFind.durata += reg.Durata_Fig.Value;
                                            }
                                            else 
                                            {
                                                ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                                dataToExport.Add(oggToAdd);
                                            }
                                        }
                                    }
                                    else if (!cants.First().Descrizione_Can.Contains("COMUNE LIMONE"))
                                    {
                                        if (dataToExport.Count == 0)
                                        {
                                            ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                            dataToExport.Add(oggToAdd);
                                        }
                                        else
                                        {
                                            ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                            if (oggToFind != default(ExportColCantDur))
                                            {
                                                oggToFind.durata += reg.Durata_Fig.Value;
                                            }
                                            else
                                            {
                                                ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                                dataToExport.Add(oggToAdd);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            foreach (var exportReg in exportRegVsHotel)
            {
                Col collaboratore = RepoManager.ColRepo.FirstOrDefault(c => c.Col_Id == exportReg.Key);
                if (!collaboratori.Contains(collaboratore))
                {
                    collaboratori.Add(collaboratore);
                }
                for (DateTime cond = startMonth; cond.Month <= endMonth.Month && cond.Year == endMonth.Year; cond = cond.AddDays(1))
                {
                    //creo un dictionary per immagazzinare le ore, la prima key sara la descrizione del cantiere, la seconda l'attivita e l'intero il totale delle ore
                    List<Dictionary<string, Dictionary<string, int>>> listaAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
                    //vado a fare un foreach
                    var list = exportReg.Where(r => r.Data_Reg.Value == cond);
                    foreach (var reg in list)
                    {
                        if (reg.Cant_Id != null && reg.Durata_Fig != null)
                        {
                            List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                            if (cants.Count > 0)
                            {
                                if (dataToExport.Count == 0)
                                {
                                    ExportColCantDur oggToAdd = new ExportColCantDur(cants.First().Descrizione_Can, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                    dataToExport.Add(oggToAdd);
                                }
                                else
                                {
                                    ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == cants.First().Descrizione_Can && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                    if (oggToFind != default(ExportColCantDur))
                                    {
                                        oggToFind.durata += reg.Durata_Fig.Value;
                                    }
                                    else
                                    {
                                        ExportColCantDur oggToAdd = new ExportColCantDur(cants.First().Descrizione_Can, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                        dataToExport.Add(oggToAdd);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            exportRegVs = exportRegVsLimone.GroupBy(c => c.Col_Id);
            List<Reg_V> returnRegs = new List<Reg_V>();
            
            foreach (var exportReg in exportRegVs)
            {
                Col collaboratore = RepoManager.ColRepo.FirstOrDefault(c => c.Col_Id == exportReg.Key);
                var colregs = exportReg.GroupBy(r => r.Data_Reg.Value.Date);
                List<Reg_V> arrotRegs = new List<Reg_V>();
                List<Reg> arrotReg = new List<Reg>();
                foreach (var dayRegs in colregs)
                {
                    if (dayRegs.Key != null)
                    {
                        var regs = dayRegs.OrderBy(r => r.Data_Ora_Fis_E).ToList();
                        //Per ogni giorno creo una variabile temporanea e mezzogiorno
                        DateTime midDay = new DateTime(dayRegs.Key.Year, dayRegs.Key.Month, dayRegs.Key.Day, 12, 0, 0);
                        Reg_V start = new Reg_V();
                        DateTime lastDate = new DateTime();
                        foreach (var reg in regs)
                        {
                            //Se la reg è la prima ma non l'ultima (non è 1) provedo a valorizzare la variabile temporanea
                            if (reg == regs.First() && reg != regs.Last())
                            {
                                start = reg;
                            }
                            //Se la reg è la prima ed è l'ultima vuol dire che è solo una e popolo la lista di ritorno
                            else if (reg == regs.First() && reg == regs.Last())
                            {
                                List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                if (cants.First().Tipo_Interv_Can != null)
                                {
                                    string tipoInt = cants.First().Tipo_Interv_Can;
                                    Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                    if (att != default(Tab_Decod))
                                    {
                                        var tmpE = reg.Data_Ora_Fis_E;
                                        var tmpU = reg.Data_Ora_Fis_U;
                                        reg.Data_Reg = dayRegs.Key;
                                        reg.Note_Reg = att.Campo1_Tab;
                                        arrotRegs.Add(reg);
                                        arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                        foreach (Reg regV in arrotReg)
                                        {
                                            reg.Durata_Fig += regV.Rettifica_Durata;
                                        }
                                        if (dataToExport.Count == 0)
                                        {
                                            ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                            dataToExport.Add(oggToAdd);
                                        }
                                        else
                                        {
                                            ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                            if (oggToFind != default(ExportColCantDur))
                                            {
                                                oggToFind.durata += reg.Durata_Fig.Value;
                                            }
                                            else
                                            {
                                                ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, reg.Durata_Fig.Value);
                                                dataToExport.Add(oggToAdd);
                                            }
                                        }
                                    }
                                }
            
                            }
                            //Se la reg non è la prima ma siamo all'ultimo controllo se la variabile temporanea è valorizzata, altrimenti metto la reg singola
                            else if (reg != regs.First() && reg == regs.Last())
                            {
                                if (start != null)
                                {
                                    if (start.Data_Ora_Fis_E < midDay && reg.Data_Ora_Fis_E > midDay)
                                    {
                                        List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                        if (cants.First().Tipo_Interv_Can != null)
                                        {
                                            string tipoInt = cants.First().Tipo_Interv_Can;
                                            Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                            if (att != default(Tab_Decod))
                                            {
                                                var tmpE = reg.Data_Ora_Fis_E;
                                                var tmpU = reg.Data_Ora_Fis_U;
                                                Reg_V returnReg = reg;
                                                returnReg.Data_Ora_Fis_E = start.Data_Ora_Fis_E;
                                                returnReg.Data_Ora_Fis_U = lastDate;
                                                returnReg.Durata_Fig = (int)((lastDate - start.Data_Ora_Fis_E).TotalMinutes);
                                                returnReg.Durata_Fis = (int)((lastDate - start.Data_Ora_Fis_E).TotalMinutes);
                                                returnReg.Data_Reg = dayRegs.Key;
                                                returnReg.Note_Reg = att.Campo1_Tab;
                                                arrotRegs.Add(returnReg);
                                                arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                                foreach (Reg regV in arrotReg)
                                                {
                                                    returnReg.Durata_Fig += regV.Rettifica_Durata;
                                                }
                                                if (dataToExport.Count == 0)
                                                {
                                                    ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                    dataToExport.Add(oggToAdd);
                                                }
                                                else
                                                {
                                                    ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                                    if (oggToFind != default(ExportColCantDur))
                                                    {
                                                        oggToFind.durata += reg.Durata_Fig.Value;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                        dataToExport.Add(oggToAdd);
                                                    }
                                                }
                                                reg.Data_Ora_Fis_E = tmpE;
                                                reg.Data_Ora_Fis_U = tmpU;
                                                start = reg;
                                                arrotRegs = new List<Reg_V>();
                                            }
                                        }
            
                                    }
                                    else
                                    {
                                        List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                        if (cants.First().Tipo_Interv_Can != null)
                                        {
                                            string tipoInt = cants.First().Tipo_Interv_Can;
                                            Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                            if (att != default(Tab_Decod))
                                            {
                                                var tmpE = reg.Data_Ora_Fis_E;
                                                var tmpU = reg.Data_Ora_Fis_U;
                                                DateTime? end = reg.Data_Ora_Fis_U;
                                                if (end == null)
                                                {
                                                    end = reg.Data_Ora_Fis_E;
                                                }
                                                Reg_V returnReg = reg;
                                                returnReg.Data_Ora_Fis_E = start.Data_Ora_Fis_E;
                                                returnReg.Data_Ora_Fis_U = reg.Data_Ora_Fis_U;
                                                returnReg.Durata_Fig = (int)(end - start.Data_Ora_Fis_E).Value.TotalMinutes;
                                                returnReg.Durata_Fis = (int)(end - start.Data_Ora_Fis_E).Value.TotalMinutes;
                                                returnReg.Data_Reg = dayRegs.Key;
                                                returnReg.Note_Reg = att.Campo1_Tab;
                                                arrotRegs.Add(returnReg);
                                                arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                                foreach (Reg regV in arrotReg)
                                                {
                                                    returnReg.Durata_Fig += regV.Rettifica_Durata;
                                                }
                                                if (dataToExport.Count == 0)
                                                {
                                                    ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                    dataToExport.Add(oggToAdd);
                                                }
                                                else
                                                {
                                                    ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                                    if (oggToFind != default(ExportColCantDur))
                                                    {
                                                        oggToFind.durata += reg.Durata_Fig.Value;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                        dataToExport.Add(oggToAdd);
                                                    }
                                                }
                                                reg.Data_Ora_Fis_E = tmpE;
                                                reg.Data_Ora_Fis_U = tmpU;
                                                start = reg;
                                                arrotRegs = new List<Reg_V>();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                    if (cants.First().Tipo_Interv_Can != null)
                                    {
                                        string tipoInt = cants.First().Tipo_Interv_Can;
                                        Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                        if (att != default(Tab_Decod))
                                        {
                                            var tmpE = reg.Data_Ora_Fis_E;
                                            var tmpU = reg.Data_Ora_Fis_U;
                                            reg.Data_Reg = dayRegs.Key;
                                            reg.Note_Reg = att.Campo1_Tab;
                                            arrotRegs.Add(reg);
                                            arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                            foreach (Reg regV in arrotReg)
                                            {
                                                reg.Durata_Fig += regV.Rettifica_Durata;
                                            }
                                            returnRegs.Add(reg);
                                            reg.Data_Ora_Fis_E = tmpE;
                                            reg.Data_Ora_Fis_U = tmpU;
                                            start = reg;
                                            arrotRegs = new List<Reg_V>();
                                        }
                                    }
                                }
                            }
                            else if (reg != regs.First() && reg != regs.Last())
                            {
                                if (start == null)
                                {
                                    start = reg;
                                }
                                else
                                {
                                    if (start.Data_Ora_Fis_E < midDay && reg.Data_Ora_Fis_E > midDay)
                                    {
                                        List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                        if (cants.First().Tipo_Interv_Can != null)
                                        {
                                            string tipoInt = cants.First().Tipo_Interv_Can;
                                            Tab_Decod att = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_INTERVENTO" && td.Chiave_Tab == tipoInt);
                                            if (att != default(Tab_Decod))
                                            {
                                                var tmpE = reg.Data_Ora_Fis_E;
                                                var tmpU = reg.Data_Ora_Fis_U;
                                                Reg_V returnReg = reg;
                                                returnReg.Data_Ora_Fis_E = start.Data_Ora_Fis_E;
                                                returnReg.Data_Ora_Fis_U = lastDate;
                                                returnReg.Durata_Fig = (int)((lastDate - start.Data_Ora_Fis_E).TotalMinutes);
                                                returnReg.Durata_Fis = (int)((lastDate - start.Data_Ora_Fis_E).TotalMinutes);
                                                returnReg.Data_Reg = dayRegs.Key;
                                                returnReg.Note_Reg = att.Campo1_Tab;
                                                arrotRegs.Add(returnReg);
                                                arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                                foreach (Reg regV in arrotReg)
                                                {
                                                    returnReg.Durata_Fig += regV.Rettifica_Durata;
                                                }
                                                if (dataToExport.Count == 0)
                                                {
                                                    ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                    dataToExport.Add(oggToAdd);
                                                }
                                                else
                                                {
                                                    ExportColCantDur oggToFind = dataToExport.FirstOrDefault(d => d.descrizioneCant == att.Campo1_Tab && d.cognomeCol == collaboratore.Cognome_Col && d.nomeCol == collaboratore.Nome_Col);
                                                    if (oggToFind != default(ExportColCantDur))
                                                    {
                                                        oggToFind.durata += reg.Durata_Fig.Value;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantDur oggToAdd = new ExportColCantDur(att.Campo1_Tab, collaboratore.Cognome_Col, collaboratore.Nome_Col, returnReg.Durata_Fig.Value);
                                                        dataToExport.Add(oggToAdd);
                                                    }
                                                }
                                                reg.Data_Ora_Fis_E = tmpE;
                                                reg.Data_Ora_Fis_U = tmpU;
                                                start = reg;
                                                arrotRegs = new List<Reg_V>();
                                            }
                                        }
                                    }
                                }
                            }
                            if (reg.Data_Ora_Fis_U != null)
                            {
                                lastDate = reg.Data_Ora_Fis_U.Value;
                            }
                            else
                            {
                                lastDate = reg.Data_Ora_Fis_E;
                            }
                        }
                    }
                }
            }
            
            regVs.AddRange(returnRegs);

            var groupedEntities = dataToExport.GroupBy(de => de.descrizioneCant).ToList();

            foreach (var entities in groupedEntities) 
            {
                int startRowIndex = rowIndex;
                foreach (var entity in entities) 
                {
                    RangeSetBorders(1,1, rowIndex,9, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetBorders(1,8, rowIndex,8, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyleThick);
                    CellInsertValue(1,1,rowIndex,entity.cognomeCol,ExcelInsertTypeEnum.Content);
                    CellInsertValue(1,2,rowIndex,entity.nomeCol,ExcelInsertTypeEnum.Content);
                    CellInsertValue(1,3,rowIndex,entity.descrizioneCant, ExcelInsertTypeEnum.Content);
                    TimeSpan durata = TimeSpan.FromMinutes(entity.durata);
                    int minuti = 00;
                    //switch (durata.Minutes)
                    //{
                    //    case 15:
                    //        minuti = 25;
                    //        break;
                    //    case 30:
                    //        minuti = 50;
                    //        break;
                    //    case 45:
                    //        minuti = 75;
                    //        break;
                    //}

                    if (durata.Minutes >= 15 && durata.Minutes < 30)
                    {
                        minuti = 25;
                    }
                    else if (durata.Minutes >= 30 && durata.Minutes < 45)
                    {
                        minuti = 50;
                    }
                    else if (durata.Minutes >= 45 && durata.Minutes <= 59)
                    {
                        minuti = 75;
                    }
                    string totale = String.Format("{0},{1}", (durata.Days * 24) + durata.Hours, minuti.ToString("00"));
                    CellInsertValue(1,4,rowIndex, totale, ExcelInsertTypeEnum.Content);
                    rowIndex++;
                }
                RangeSetBorders(1, 1, startRowIndex, 7, startRowIndex, borderColor, borderStyleThick, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetBorders(1, 8, startRowIndex, 8, startRowIndex, borderColor, borderStyleThick, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyleThick);
            }

            RangeSetBorders(1, 1, rowIndex - 1, 7, rowIndex - 1, borderColor, borderStyle, borderColor, borderStyleThick, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBorders(1, 8, rowIndex - 1, 8, rowIndex - 1, borderColor, borderStyle, borderColor, borderStyleThick, borderColor, borderStyle, borderColor, borderStyleThick);

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
    }
}
