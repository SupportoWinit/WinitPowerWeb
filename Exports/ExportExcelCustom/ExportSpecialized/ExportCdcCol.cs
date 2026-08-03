
using Business.BusinessExtension;
using Business.DataClasses;
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
    public class ExportCdcCol : ExcelToolbox<Reg_V>, IExportExcelCustom<Reg_V>
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
            List<Reg_V> reg_Vs = entitiesToExport.ToList();
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
            List<string> motivId = RepoManager.Tab_DecodRepo.GetAllQueryable(td => td.Campo1_Tab == "PULIZIE CIVILI" || td.Campo1_Tab == "CONDOMINIO").Select(td => td.Chiave_Tab).ToList();
            //faccio la lista vuota come da richiesta del 29/07/2026 per esportare tutti i cantieri
            List<int> cantToExclude = new List<int>();//RepoManager.CantRepo.GetAllQueryable(c => motivId.Contains(c.Tipo_Interv_Can)).Select(c => c.Cant_Id).ToList();
            List<Reg_V> regVs = reg_Vs.Where(r => !(cantToExclude.Contains(r.Cant_Id.Value)) && r.Data_Reg >= minDate && r.Data_Reg <= maxDate && r.Codice_Commessa_Can != "Hotel" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id && r.Registrazione_Stato_Reg != 0).ToList();
            //List<Reg_V> regVs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg > minDate && r.Data_Reg < maxDate && r.Qualifica_Col == "0").ToList();
            var exportRegVs = regVs.GroupBy(c => c.Data_Reg);
            regVs = new List<Reg_V>();
            List<DateTime> monthDays = CommonService.GetDatesFromPeriod(CommonService.GetFirstMonthDay(ExportPeriod), CommonService.GetLastMonthDay(ExportPeriod));
            //ordino le ore in base alla ora della registrazione e le reggruppo per i cantieri
            rowIndex = 2;
            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Tipologia_Can == "ATT").ToList();
           
            List<ExportColCant> provaLista = new List<ExportColCant>();
            string lastAtt = "";
            int lastAttId = 0;
            string lastCant = "";
            int lastDurata = 0;
            int totaleReg = 0;
            List<ExportColCantInt> numeroInterventFinale = new List<ExportColCantInt>();
            List<ExportColCantInt> numeroInterventGiornaliero = new List<ExportColCantInt>();

            foreach (var group in exportRegVs) 
            {
                foreach (Reg_V regV in group) 
                { 
                    Col collaboratore = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == regV.Col_Id).FirstOrDefault();
                    Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regV.Cant_Id).FirstOrDefault();
                    Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                    if (tipoInt != default) 
                    {
                        if (cantiere != default(Cant) && collaboratore != default(Col))
                        {
                            Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                            if (cliente != default(Cli))
                            {
                                if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                {
                                    if (cantiere.Raggruppamento1_Can != null)
                                    {
                                        string ragg = cantiere.Raggruppamento1_Can;
                                        Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                        string raggruppamento = raggDecod.Decodifica_Tab;
                                        string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                        ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                            if (regV.Registrazione_Tipo_Reg == 0)
                                                exOgg.interventi += 1;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                            if (regV.Registrazione_Tipo_Reg != 0)
                                                newOgg.interventi = 0;
                                            numeroInterventGiornaliero.Add(newOgg);
                                        }
                                    }
                                    else
                                    {
                                        ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                            if (regV.Registrazione_Tipo_Reg == 0)
                                                exOgg.interventi += 1;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                            if (regV.Registrazione_Tipo_Reg != 0)
                                                newOgg.interventi = 0;
                                            numeroInterventGiornaliero.Add(newOgg);
                                        }
                                    }

                                }
                                else if (cliente.Cognome_Cli == "COMUNE DI MALCESINE")
                                {
                                    if (cantiere.Raggruppamento1_Can != null)
                                    {
                                        string ragg = cantiere.Raggruppamento1_Can;
                                        Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                        string raggruppamento = raggDecod.Decodifica_Tab;
                                        string oggToSearch = "COMUNE DI MALCESINE - " + raggruppamento.ToUpper();
                                        ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                            if (regV.Registrazione_Tipo_Reg == 0)
                                                exOgg.interventi += 1;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                            if (regV.Registrazione_Tipo_Reg != 0)
                                                newOgg.interventi = 0;
                                            numeroInterventGiornaliero.Add(newOgg);
                                        }
                                    }
                                    else
                                    {
                                        ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                            if (regV.Registrazione_Tipo_Reg == 0)
                                                exOgg.interventi += 1;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                            if (regV.Registrazione_Tipo_Reg != 0)
                                                newOgg.interventi = 0;
                                            numeroInterventGiornaliero.Add(newOgg);
                                        }
                                    }
                                }
                                else
                                {
                                    ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                    if (exOgg != null)
                                    {
                                        exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                        if (regV.Registrazione_Tipo_Reg == 0)
                                            exOgg.interventi += 1;
                                    }
                                    else
                                    {
                                        ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                        if (regV.Registrazione_Tipo_Reg != 0)
                                            newOgg.interventi = 0;
                                        numeroInterventGiornaliero.Add(newOgg);
                                    }
                                }
                            }
                            else
                            {
                                ExportColCantInt exOgg = numeroInterventGiornaliero.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                if (exOgg != null)
                                {
                                    exOgg.durata += regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0;
                                    if (regV.Registrazione_Tipo_Reg == 0)
                                        exOgg.interventi += 1;
                                }
                                else
                                {
                                    ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fis != null ? regV.Durata_Fis.Value : 0, 1);
                                    if (regV.Registrazione_Tipo_Reg != 0)
                                        newOgg.interventi = 0;
                                    numeroInterventGiornaliero.Add(newOgg);
                                }
                            }
                        }
                    }
                }
                foreach (var intervento in numeroInterventGiornaliero) 
                {
                    intervento.durata = CommonService.ConvertDaySum(intervento.durata);
                }
                foreach (var intervento in numeroInterventGiornaliero) 
                {
                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == intervento.descrizioneCant && ex.descrizioneCol == intervento.descrizioneCol);
                    if (exOgg != null)
                    {
                        exOgg.durata += intervento.durata;
                        exOgg.interventi += intervento.interventi;
                    }
                    else
                    {
                        numeroInterventFinale.Add(intervento);
                    }
                }
                numeroInterventGiornaliero = new List<ExportColCantInt>();
            }

            foreach (var prova in regVs.GroupBy(c => c.Col_Id)) 
            {
                Col collaboratore = RepoManager.ColRepo.GetAllQueryable(c => c.Col_Id == prova.Key).FirstOrDefault();
                string initCant = "";
                int cantId = 0;
                int totalMinute = 0;
                int totalInt = 0;
                foreach (var regV in prova) 
                {
                    if (regV != prova.Last())
                    {
                        if (initCant == "")
                        {
                            initCant = regV.Cant_Desc;
                            cantId = regV.Cant_Id != null ? regV.Cant_Id.Value : 0;
                            totalMinute = regV.Durata_Fig != null ? regV.Durata_Fig.Value : 0;
                            if(regV.Registrazione_Tipo_Reg == 0)
                                totalInt++;
                        }
                        else if (initCant != regV.Cant_Desc)
                        {
                            Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == initCant).FirstOrDefault();
                            if (cantiere != default(Cant))
                            {
                                Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                                if (tipoInt != default(Tab_Decod)) 
                                {
                                    Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                                    if (cliente != default(Cli))
                                    {
                                        if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                        {
                                            if (cantiere.Raggruppamento1_Can != null)
                                            {
                                                string ragg = cantiere.Raggruppamento1_Can;
                                                Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                                string raggruppamento = raggDecod.Decodifica_Tab;
                                                string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }
                                            else 
                                            {
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }
                                        }
                                        else 
                                        {
                                            ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                            if (exOgg != null)
                                            {
                                                exOgg.durata += totalMinute;
                                                exOgg.interventi += totalInt;
                                            }
                                            else
                                            {
                                                ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                numeroInterventFinale.Add(newOgg);
                                            }
                                        }
                                    }
                                    else 
                                    {
                                        ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += totalMinute;
                                            exOgg.interventi += totalInt;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                            numeroInterventFinale.Add(newOgg);
                                        }
                                    }  
                                }       
                            }
                            initCant = regV.Cant_Desc;
                            cantId = regV.Cant_Id != null ? regV.Cant_Id.Value : 0;
                            totalInt = 1;
                            totalMinute = regV.Durata_Fig != null ? regV.Durata_Fig.Value : 0;
                        }
                        else
                        {
                            if (regV.Registrazione_Tipo_Reg == 0)
                                totalInt++;
                            totalMinute += regV.Durata_Fig != null ? regV.Durata_Fig.Value : 0;
                        }
                    }
                    else 
                    {
                        if (initCant != "")
                        {
                            if (initCant != regV.Cant_Desc)
                            {
                                Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == initCant).FirstOrDefault();
                                if (cantiere != default(Cant))
                                {
                                    Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                                    if (tipoInt != default(Tab_Decod))
                                    {
                                        Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                                        if (cliente != default(Cli))
                                        {
                                            if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                            {
                                                if (cantiere.Raggruppamento1_Can != null)
                                                {
                                                    string ragg = cantiere.Raggruppamento1_Can;
                                                    Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                                    string raggruppamento = raggDecod.Decodifica_Tab;
                                                    string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                                else 
                                                {
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                                    
                                            }
                                            else 
                                            {
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }
                                                
                                        }
                                        else
                                        {
                                            ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                            if (exOgg != null)
                                            {
                                                exOgg.durata += totalMinute;
                                                exOgg.interventi += totalInt;
                                            }
                                            else
                                            {
                                                ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                numeroInterventFinale.Add(newOgg);
                                            }
                                        }
                                    }
                                        
                                }
                                cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == regV.Cant_Desc).FirstOrDefault();
                                if (cantiere != default(Cant))
                                {
                                    Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                                    if (tipoInt != default(Tab_Decod))
                                    {
                                        Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                                        if (cliente != default(Cli))
                                        {
                                            if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                            {
                                                if (cantiere.Raggruppamento1_Can != null)
                                                {
                                                    string ragg = cantiere.Raggruppamento1_Can;
                                                    Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                                    string raggruppamento = raggDecod.Decodifica_Tab;
                                                    string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                                else 
                                                {
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                            }
                                            else 
                                            {
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            } 
                                        }
                                        else
                                        {
                                            ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                            if (exOgg != null)
                                            {
                                                exOgg.durata += totalMinute;
                                                exOgg.interventi += totalInt;
                                            }
                                            else
                                            {
                                                ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                numeroInterventFinale.Add(newOgg);
                                            }
                                        }
                                    } 
                                }
                            }
                            else 
                            {
                                totalInt++;
                                totalMinute += regV.Durata_Fig != null ? regV.Durata_Fig.Value : 0;
                                Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == initCant).FirstOrDefault();
                                if (cantiere != default(Cant))
                                {
                                    Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                                    if (tipoInt != default(Tab_Decod))
                                    {
                                        Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                                        if (cliente != default(Cli))
                                        {
                                            if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                            {
                                                if (cantiere.Raggruppamento1_Can != null)
                                                {
                                                    string ragg = cantiere.Raggruppamento1_Can;
                                                    Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                                    string raggruppamento = raggDecod.Decodifica_Tab;
                                                    string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                                else 
                                                {
                                                    ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                    if (exOgg != null)
                                                    {
                                                        exOgg.durata += totalMinute;
                                                        exOgg.interventi += totalInt;
                                                    }
                                                    else
                                                    {
                                                        ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                        numeroInterventFinale.Add(newOgg);
                                                    }
                                                }
                                            }
                                            else 
                                            {
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }  
                                        }
                                        else
                                        {
                                            ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == initCant && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                            if (exOgg != null)
                                            {
                                                exOgg.durata += totalMinute;
                                                exOgg.interventi += totalInt;
                                            }
                                            else 
                                            {
                                                ExportColCantInt newOgg = new ExportColCantInt(initCant, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                numeroInterventFinale.Add(newOgg);
                                            }
                                               
                                        }
                                    }
                                }
                            }
                        }
                        else 
                        {
                            Cant cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == regV.Cant_Desc).FirstOrDefault();
                            if (cantiere != default(Cant))
                            {
                                Tab_Decod tipoInt = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Chiave_Tab == cantiere.Tipo_Interv_Can).FirstOrDefault();
                                if (tipoInt != default(Tab_Decod))
                                {
                                    Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cantiere.Cli_Id).FirstOrDefault();
                                    if (cliente != default(Cli))
                                    {
                                        if (cliente.Cognome_Cli == "COMUNE LIMONE")
                                        {
                                            if (cantiere.Raggruppamento1_Can != null)
                                            {
                                                string ragg = cantiere.Raggruppamento1_Can;
                                                Tab_Decod raggDecod = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "RAGGRUPPAMENTO_1" && t.Chiave_Tab == ragg).FirstOrDefault();
                                                string raggruppamento = raggDecod.Decodifica_Tab;
                                                string oggToSearch = "COMUNE LIMONE - " + raggruppamento.ToUpper();
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == oggToSearch && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(oggToSearch, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, totalMinute, totalInt);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }
                                            else 
                                            {
                                                ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                                if (exOgg != null)
                                                {
                                                    exOgg.durata += totalMinute;
                                                    exOgg.interventi += totalInt;
                                                }
                                                else
                                                {
                                                    ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fig.Value, 1);
                                                    numeroInterventFinale.Add(newOgg);
                                                }
                                            }
                                        }
                                        else 
                                        {
                                            ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                            if (exOgg != null)
                                            {
                                                exOgg.durata += totalMinute;
                                                exOgg.interventi += totalInt;
                                            }
                                            else
                                            {
                                                ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fig.Value, 1);
                                                numeroInterventFinale.Add(newOgg);
                                            }
                                        }   
                                    }
                                    else
                                    {

                                        ExportColCantInt exOgg = numeroInterventFinale.FirstOrDefault(ex => ex.descrizioneCant == cantiere.Descrizione_Can && ex.descrizioneCol == collaboratore.CognomeNome_Col);
                                        if (exOgg != null)
                                        {
                                            exOgg.durata += totalMinute;
                                            exOgg.interventi += totalInt;
                                        }
                                        else
                                        {
                                            ExportColCantInt newOgg = new ExportColCantInt(cantiere.Descrizione_Can, collaboratore.CognomeNome_Col, tipoInt.Decodifica_Tab, regV.Durata_Fig.Value, 1);
                                            numeroInterventFinale.Add(newOgg);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            foreach (var prova in numeroInterventFinale.OrderBy(p => p.descrizioneCant))
            {
                List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Descrizione_Can == prova.descrizioneCant).ToList();
                Tab_Decod att = RepoManager.Tab_DecodRepo.GetAllQueryable(t => t.Nome_Tab == "TIPO_INTERVENTO" && t.Decodifica_Tab == prova.descrizioneAtt).FirstOrDefault();
                string descrizioneCliente = "";
                if (cants.Count() > 0)
                {
                    if (cants.Last().Cli_Id != null)
                    {
                        int cliId = cants.Last().Cli_Id.Value;
                        Cli cliente = RepoManager.CliRepo.GetAllQueryable(cl => cl.Cli_Id == cliId).FirstOrDefault();
                        if (cliente != default(Cli))
                        {
                            descrizioneCliente = cliente.Cognome_Cli;
                        }
                    }
                    else 
                    {
                        if (cants.First().Descrizione_Can.Contains("LIMONE")) 
                        {
                            descrizioneCliente = "COMUNE LIMONE";
                        }
                    }
                }
                else 
                {
                    if (prova.descrizioneCant.Contains("LIMONE"))
                    {
                        descrizioneCliente = "COMUNE LIMONE";
                    }
                    else
                    {
                        descrizioneCliente = "COMUNE DI MALCESINE";
                    }
                }
                CellInsertValue(1, 1, rowIndex, descrizioneCliente + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 1, rowIndex, 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 1, rowIndex, 1, rowIndex, 11);
                RangeSetWrapText(1, 1, rowIndex, 1, rowIndex, true);

                CellInsertValue(1, 2, rowIndex, prova.descrizioneCant + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 2, rowIndex, 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 2, rowIndex, 2, rowIndex, 11);
                RangeSetWrapText(1, 2, rowIndex, 2, rowIndex, true);

                if (att != default(Tab_Decod))
                {
                    CellInsertValue(1, 3, rowIndex, att.Campo1_Tab + " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);
                    RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                }
                else 
                {
                    CellInsertValue(1, 3, rowIndex, " ", ExcelInsertTypeEnum.Content);
                    RangeSetBorders(1, 3, rowIndex, 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    RangeSetFontSize(1, 3, rowIndex, 3, rowIndex, 11);
                    RangeSetWrapText(1, 3, rowIndex, 3, rowIndex, true);
                }


                CellInsertValue(1, 4, rowIndex, prova.descrizioneAtt + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 4, rowIndex, 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 4, rowIndex, 4, rowIndex, 11);
                RangeSetWrapText(1, 4, rowIndex, 4, rowIndex, true);

                CellInsertValue(1, 5, rowIndex, prova.descrizioneCol + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 5, rowIndex, 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 5, rowIndex, 5, rowIndex, 11);
                RangeSetWrapText(1, 5, rowIndex, 5, rowIndex, true);

                TimeSpan durata = TimeSpan.FromMinutes(prova.durata);
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
                CellInsertValue(1, 6, rowIndex, totale + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 6, rowIndex, 6, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 6, rowIndex, 6, rowIndex, 11);
                RangeSetWrapText(1, 6, rowIndex, 6, rowIndex, true);

                CellInsertValue(1, 7, rowIndex, prova.interventi + " ", ExcelInsertTypeEnum.Content);
                RangeSetBorders(1, 7, rowIndex, 7, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                RangeSetFontSize(1, 7, rowIndex, 7, rowIndex, 11);
                RangeSetWrapText(1, 7, rowIndex, 7, rowIndex, true);

                rowIndex++;
            }

            ExcelWorkbookSaveToResponse(HttpContext.Current.Response, System.IO.Path.GetFileName(ExcelModelFilePath), true);

            ExcelWorkbookDispose();
        }

        private void WriteTimesheetColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportPeriod, ExportPeriod.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(1, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                //WriteHeaderDayCell(day);

                WriteHeaderDayCellDay(day);

                rowIndex++;
            });

            WriteHeaderTotalCell();

            rowIndex++;

            WriteHeaderTotalDaysCell();

            rowIndex++;

            //WriteHeaderTotalFest();

            //columnIndex++;

            //WriteHeaderTotalFer();

            columnIndex = 2;

            rowIndex++;
        }

        private void WriteHeaderDayCellDay(DateTime date)
        {
            String giorno = "";
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    giorno = "L " + date.Day;
                    break;
                case DayOfWeek.Tuesday:
                    giorno = "MA " + date.Day;
                    break;
                case DayOfWeek.Wednesday:
                    giorno = "ME " + date.Day;
                    break;
                case DayOfWeek.Thursday:
                    giorno = "G " + date.Day;
                    break;
                case DayOfWeek.Friday:
                    giorno = "V " + date.Day;
                    break;
                case DayOfWeek.Saturday:
                    giorno = "S " + date.Day;
                    break;
                case DayOfWeek.Sunday:
                    giorno = "D " + date.Day;
                    break;
            }

            CellInsertValue(1, rowIndex + 1, columnIndex, giorno, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, "0");
            //RangeSetBackgroundColor(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day, Color.LightGray, fillStyle);
            RangeSetFontSize(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, 12);
            RangeSetWrapText(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1, true);
            ColumnsSetWidth(1, rowIndex + 1, rowIndex + 1, 8);

            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, date.Day + 1);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetFontBold(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex);

        }


        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(1, rowIndex, columnIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex, columnIndex, rowIndex, date.Day, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(1, rowIndex, columnIndex, rowIndex, date.Day, "0");
            RangeSetBackgroundColor(1, rowIndex, columnIndex, rowIndex, date.Day, Color.LightGray, fillStyle);
            RangeSetWrapText(1, rowIndex, columnIndex, rowIndex, date.Day, true);
            ColumnsSetWidth(1, rowIndex, rowIndex, 6);
            RangeSetFontBold(1, rowIndex, columnIndex, rowIndex, date.Day);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(1, rowIndex, columnIndex, rowIndex, columnIndex, Color.Red);
            }
        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(1, rowIndex + 1, columnIndex, "Tot.G", ExcelInsertTypeEnum.Content);
            RangeSetBorders(1, rowIndex + 1, columnIndex, rowIndex + 1, columnIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);

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
