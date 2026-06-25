using Business.BusinessExtension;
using Business.Repository;
using Common;
using DevExpress.XtraCharts.Native;
using Domain;
using log4net;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Westwind.Utilities.Extensions;
using static Business.MDBSchema.PowerMDBDataSet;

namespace Exports.ExportExcelCustom.ExportSpecialized
{
    /*
     *
     *      Attenzione non gestisce i totali settimanali
     * 
     */

    public class ExportCdcManutentori : ExcelToolBox
    {

        #region Constants

        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTimesheetSimple));

        private int worksheetIndex = 0;
        private int rowIndex = 1;
        private int columnIndex = 1;
        private int giorno = 2;


        private OfficeOpenXml.Style.ExcelBorderStyle borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        private OfficeOpenXml.Style.ExcelFillStyle fillStyle = OfficeOpenXml.Style.ExcelFillStyle.Solid;

        private Color borderColor = Color.Black;

        private DateTime startMonth;
        private DateTime endMonth;

        private bool _multiPagedExport = true;// Convert.ToBoolean(RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TimesheetMultiPagedExport));

        #endregion

        #region Public Methods

        public override void LaunchExport()
        {
            Param parameters = RepoManager.ParamRepo.ParametersRow;

            Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>> cartellini = new Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>();
            Dictionary<string, Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>> centri = new Dictionary<string, Dictionary<Col, Dictionary<string, List<TimesheetModuleItem>>>>();

            List<CentroDiCosto> cdc = RepoManager.CentroDiCostoRepo.GetAll().ToList();
            List<Reg_V> regVs = new List<Reg_V>();

            startMonth = CommonService.GetFirstMonthDay(ExportDate);
            endMonth = CommonService.GetLastMonthDay(ExportDate);

            Tab_Decod motivazionePausa = RepoManager.Tab_DecodRepo.Single(d => d.Chiave_Tab == "Pausa");
            var collaboratori = new List<Col>();
            List<Reg_V> exportRegs = new List<Reg_V>();
            List<Col> manutentori = RepoManager.ColRepo.GetAllQueryable(c => c.Qualifica_Col == "0").ToList();

            //foreach (Col collaboratore in manutentori) 
            //{
            //    if (collaboratore.Scadenza_Patente_Col != null)
            //    {
            //        if (collaboratore.Scadenza_Patente_Col.Value.Between(startMonth, endMonth))
            //        {
            //            exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= collaboratore.Scadenza_Patente_Col.Value && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            //            collaboratori.Add(collaboratore);
            //        }
            //        else
            //        {
            //            exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            //            collaboratori.Add(collaboratore);
            //        }
            //    }
            //    else 
            //    {
            //        exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            //        collaboratori.Add(collaboratore);
            //    }                
            //}

            List<Col> colCambiati = RepoManager.ColRepo.GetAllQueryable(c => c.Qualifica_Col != "0" && c.Scadenza_Patente_Col.Value != null).ToList();

            //foreach (Col collaboratore in colCambiati)
            //{
            //    if (collaboratore.Scadenza_Patente_Col != null)
            //    {
            //        if (collaboratore.Scadenza_Patente_Col.Value.Between(startMonth, endMonth))
            //        {
            //            exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id && r.Data_Reg >= startMonth && r.Data_Reg <= collaboratore.Scadenza_Patente_Col.Value && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            //            collaboratori.Add(collaboratore);
            //        }
            //    }
            //}
            Cli cliente = RepoManager.CliRepo.FirstOrDefault(cl => cl.Cognome_Cli == "COMUNE LIMONE");
            int cliId = cliente.Cli_Id;
            List<Reg_V> regVs2 = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && r.Qualifica_Col == "0" && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList();
            exportRegs.AddRange(RepoManager.Reg_VRepo.GetAllQueryable(r => r.Codice_Commessa_Can == "Pulizie Civile" && r.Data_Reg >= startMonth && r.Data_Reg <= endMonth && (r.Registrazione_Tipo_Reg == 0 || r.Registrazione_Tipo_Reg == 2 || r.Registrazione_Tipo_Reg == 4 || r.Registrazione_Tipo_Reg == 10) && r.Motivazione_Reg_Id != motivazionePausa.Tab_Decod_Id).ToList());
            var exportRegVsLimone = exportRegs.Where(r => r.CentroDiCosto_Id == 6 && r.Registrazione_Tipo_Reg == 0);
            exportRegs = exportRegs.Where(r => r.CentroDiCosto_Id != 6).ToList();
            var exportRegVs = exportRegs.GroupBy(c => c.Col_Id); 
            collaboratori = new List<Col>();
            DateTime exportDate = new DateTime(2026, 04, 01);
            if (startMonth < exportDate)
            {
                foreach (var exportReg in exportRegVs)
                {
                    for (DateTime cond = startMonth; cond.Month <= endMonth.Month && cond.Year == endMonth.Year; cond = cond.AddDays(1))
                    {
                        //creo un dictionary per immagazzinare le ore, la prima key sara la descrizione del cantiere, la seconda l'attivita e l'intero il totale delle ore
                        List<Dictionary<string, Dictionary<string, int>>> listaAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
                        string lastAtt = "";
                        int lastAttId = 0;
                        string lastCant = "";
                        int lastDurata = 1;
                        //vado a fare un foreac
                        var list = exportReg.Where(r => r.Data_Reg.Value == cond);
                        int totaleReg = 0;
                        foreach (var reg in list.OrderBy(r => r.Col_Id).ThenBy(r => r.Data_Ora_Fis_E))
                        {
                            List<Dictionary<string, Dictionary<string, int>>> tmpAttivita = new List<Dictionary<string, Dictionary<string, int>>>();
                            List<Cant> currentCant = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                            //controllo se la timbratura è un attività
                            if (reg.Registrazione_Tipo_Reg == 2)
                            {
                                //recupero la lista delle attività
                                List<Cant> attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
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
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                            lastDurata = 0;
                                            aggiornato = true;
                                        }
                                        else
                                        {
                                            if (lastDurata > 0)
                                            {
                                                Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                                tmpDi[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpDi);
                                            }
                                            aggiornato = true;
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                            lastDurata = 0;
                                        }
                                        if (!aggiornato)
                                        {
                                            if (lastDurata > 0)
                                            {
                                                var tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                                tmpdic[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpdic);
                                            }
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                        }
                                    }
                                    else
                                    {
                                        if (lastDurata > 0)
                                        {
                                            Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                            tmpDi[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                            tmpAttivita.Add(tmpDi);
                                        }
                                        lastDurata = 0;
                                        lastAtt = attivita.First().Note_Can;
                                        lastAttId = attivita.First().Cant_Id;
                                    }

                                }
                                else
                                {
                                    if (lastAtt != "" && lastDurata > 0)
                                    {
                                        attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Note_Can == lastAtt).ToList();
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
                                            lastAtt = "";
                                            lastAttId = 0;
                                            lastDurata = 0;
                                            aggiornato = true;
                                        }
                                        else
                                        {
                                            if (lastDurata > 0)
                                            {
                                                Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                                tmpDi[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpDi);
                                            }
                                            aggiornato = true;
                                            lastAtt = "";
                                            lastAttId = 0;
                                            lastDurata = 0;
                                        }
                                        if (!aggiornato)
                                        {
                                            if (lastDurata > 0)
                                            {
                                                Dictionary<string, Dictionary<string, int>> tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                                tmpdic[currentCant.First().Note_Can] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpdic);
                                            }
                                            lastAtt = "";
                                            lastAttId = 0;
                                            lastDurata = 0;
                                        }
                                    }
                                    attivita = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == reg.Cant_Id).ToList();
                                    lastAtt = attivita.First().Note_Can;
                                    lastAttId = attivita.First().Cant_Id;
                                }
                            }
                            else if (reg.Registrazione_Tipo_Reg == 0)
                            {
                                if (reg.Durata_Fig != null)
                                {
                                    //Cant cantiere = RepoManager.CantRepo.Single(c => c.Cant_Id == reg.Cant_Id);
                                    lastDurata += reg.Durata_Fig.Value;
                                    lastCant = reg.Cant_Desc;
                                    //lastAtt = cantiere.Note_Can;
                                }
                                if (totaleReg + 1 == list.Count() && lastAtt != "")
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
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                            lastDurata = 0;
                                            aggiornato = true;
                                        }
                                        else
                                        {
                                            if (lastDurata > 0)
                                            {
                                                Dictionary<string, Dictionary<string, int>> tmpDi = new Dictionary<string, Dictionary<string, int>>();
                                                tmpDi[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpDi);
                                            }
                                            aggiornato = true;
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                            lastDurata = 0;
                                        }
                                        if (!aggiornato)
                                        {
                                            if (lastDurata > 0)
                                            {
                                                var tmpdic = new Dictionary<string, Dictionary<string, int>>();
                                                tmpdic[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                                tmpAttivita.Add(tmpdic);
                                            }
                                            lastAtt = attivita.First().Note_Can;
                                            lastAttId = attivita.First().Cant_Id;
                                        }
                                    }
                                    else
                                    {
                                        if (lastDurata > 0)
                                        {
                                            Dictionary<string, Dictionary<string, int>> tmpDic = new Dictionary<string, Dictionary<string, int>>();
                                            tmpDic[lastAtt] = new Dictionary<string, int>() { { attivita.First().Note_Can, lastDurata } };
                                            tmpAttivita.Add(tmpDic);
                                        }
                                        lastDurata = 0;
                                        lastAtt = attivita.First().Note_Can;
                                        lastAttId = attivita.First().Cant_Id;
                                    }
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
                            listaAttivita.AddRange(tmpAttivita);
                            totaleReg++;
                        }
                        if (listaAttivita.Count() > 0)
                        {
                            foreach (var cant in listaAttivita)
                            {
                                foreach (var att in cant.First().Value)
                                {
                                    List<Cant> cants = RepoManager.CantRepo.GetAllQueryable(c => c.Note_Can == att.Key).ToList();
                                    var newRounding = new Reg_V();
                                    newRounding.Col_Id = exportReg.Key;
                                    newRounding.Cant_Id = cants.First().Cant_Id;
                                    newRounding.Durata_Fig = att.Value;
                                    newRounding.Durata_Fis = att.Value;
                                    newRounding.Data_Reg = cond;
                                    regVs.Add(newRounding);
                                }
                            }
                        }

                    }
                }
            }
            else
            {
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
                            if (reg.Cant_Id != null) 
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
                                            var newRounding = new Reg_V();
                                            newRounding.Col_Id = exportReg.Key;
                                            newRounding.Cant_Id = reg.Cant_Id;
                                            newRounding.Durata_Fig = reg.Durata_Fig;
                                            newRounding.Durata_Fis = reg.Durata_Fis;
                                            newRounding.Data_Reg = cond;
                                            newRounding.Note_Reg = att.Campo1_Tab;
                                            regVs.Add(newRounding);
                                        }
                                        else if (!cants.First().Descrizione_Can.Contains("COMUNE LIMONE")) 
                                        {
                                            var newRounding = new Reg_V();
                                            newRounding.Col_Id = exportReg.Key;
                                            newRounding.Cant_Id = reg.Cant_Id;
                                            newRounding.Durata_Fig = reg.Durata_Fig;
                                            newRounding.Durata_Fis = reg.Durata_Fis;
                                            newRounding.Data_Reg = cond;
                                            newRounding.Note_Reg = att.Campo1_Tab;
                                            regVs.Add(newRounding);
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
                                            returnRegs.Add(reg);
                                            reg.Data_Ora_Fis_E = tmpE;
                                            reg.Data_Ora_Fis_U = tmpU;
                                            start = reg;
                                            arrotRegs = new List<Reg_V>();
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
                                                    returnRegs.Add(returnReg);
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
                                                    Reg_V returnReg = reg;
                                                    returnReg.Data_Ora_Fis_E = start.Data_Ora_Fis_E;
                                                    returnReg.Data_Ora_Fis_U = reg.Data_Ora_Fis_U;
                                                    returnReg.Durata_Fig = (int)(reg.Data_Ora_Fis_U - start.Data_Ora_Fis_E).Value.TotalMinutes;
                                                    returnReg.Durata_Fis = (int)(reg.Data_Ora_Fis_U - start.Data_Ora_Fis_E).Value.TotalMinutes;
                                                    returnReg.Data_Reg = dayRegs.Key;
                                                    returnReg.Note_Reg = att.Campo1_Tab;
                                                    arrotRegs.Add(returnReg);
                                                    arrotReg = RepoManager.Reg_VRepo.DurationRoundingExport(arrotRegs, RoundingMethodEnum.Duration);
                                                    foreach (Reg regV in arrotReg)
                                                    {
                                                        returnReg.Durata_Fig += regV.Rettifica_Durata;
                                                    }
                                                    returnRegs.Add(returnReg);
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
                                                    returnRegs.Add(returnReg);
                                                    reg.Data_Ora_Fis_E = tmpE;
                                                    reg.Data_Ora_Fis_U = tmpU;
                                                    start = reg;
                                                    arrotRegs = new List<Reg_V>();
                                                }
                                            }      
                                        }
                                    }
                                }
                                lastDate = reg.Data_Ora_Fis_U.Value;
                            }
                        }
                    }
                }

                regVs.AddRange(returnRegs);
            }

            foreach (Col col in collaboratori)
            {
                if (col != null) 
                { 
                    IEnumerable<int> lis = new List<int>();
                    lis = RepoManager.RegRepo.GetRegsIdByDateRangeByColNotBlocked(startMonth, endMonth, col.Col_Id);
                    if (regVs.Where(r => r.Col_Id == col.Col_Id).Count() > 0 /*|| RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CollabNoHours) == 1*/)
                    {
                        cartellini.Add(col, TimesheetModuleItem.GenerateCartellinoCartellinoCentri(ExportDate,
                                                       col,
                                                       regVs.Where(r => r.Col_Id == col.Col_Id).ToList(),
                                                       isByOtherEntity: true,
                                                       calculateWorkedHours: parameters.Cartellino_Visualizza_Ore,
                                                       calculateJustifications: parameters.Cartellino_Visualizza_Motivazioni,
                                                       calculateTrips: parameters.Cartellino_Visualizza_Viaggi,
                                                       calculateDelta: parameters.Cartellino_Visualizza_Delta,
                                                       calculateOrdStrTimesheet: false,
                                                       devidePlanByDayNight: parameters.Cartellino_Divisione_Piano_Notturno_Diurno,
                                                       showWeeklyTotal: false,
                                                       insertCorrectionRow: false));
                    }
                }
                
            }

            cartellini = cartellini.OrderBy(c => c.Key.CognomeNome_Col).ToDictionary(c => c.Key, d => d.Value);

            // per prima cosa si procede all'apertura del modello
            ExcelWorkbookGenerateNew();


            String mese = "";
            switch (ExportDate.Month)
            {
                case 1:
                    mese = "Gennaio";
                    break;
                case 2:
                    mese = "Febbraio";
                    break;
                case 3:
                    mese = "Marzo";
                    break;
                case 4:
                    mese = "Aprile";
                    break;
                case 5:
                    mese = "Maggio";
                    break;
                case 6:
                    mese = "Giugno";
                    break;
                case 7:
                    mese = "Luglio";
                    break;
                case 8:
                    mese = "Agosto";
                    break;
                case 9:
                    mese = "Settembre";
                    break;
                case 10:
                    mese = "Ottobre";
                    break;
                case 11:
                    mese = "Novembre";
                    break;
                case 12:
                    mese = "Dicembre";
                    break;
            }
            if (!_multiPagedExport)
            {
                //Not multipagedExport
                WorksheetCreateNew(mese + " " + ExportDate.Year);
                worksheetIndex++;
            }

            // una volta generato il file, se ci sono elementi da processare
            if (cartellini.Any())
            {
                foreach (var col in cartellini)
                {
                    if (_multiPagedExport)
                    {
                        ExcelWorkbook.Workbook.Worksheets.Add(col.Key.CognomeNome_Col);
                        worksheetIndex++;

                        WriteTimesheetHeader();

                        rowIndex += 2;
                    }

                    WriteTimesheetColName(col.Key);

                    WriteTimesheetColHeader();

                    WriteColTimesheet(col.Value);

                    rowIndex += 2;

                    if (parameters.Cartellino_Usa_Cartellino_Modificabile)
                    {
                        WriteStrTimesheetTitle();

                        WriteTimesheetColHeader();

                        WriteStrTimesheet(col.Value);

                        rowIndex += 2;
                    }

                    if (_multiPagedExport)
                    {
                        rowIndex = 1;
                        columnIndex = 1;
                    }
                    else
                    {
                        rowIndex += 2;
                    }
                    
                }

                foreach (var worksheet in ExcelWorkbook.Workbook.Worksheets)
                {
                    //worksheet.Cells.AutoFitColumns();
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteTimesheetColHeader()
        {
            List<DateTime> days = CommonService.GetDatesFromPeriod(ExportDate, ExportDate.AddMonths(1).AddDays(-1)); //Calcolo i giorni per l'header

            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, days.Count + 3, rowIndex);

            days.ForEach(day =>
            {
                WriteHeaderDayCell(day);

                columnIndex++;
            });

            WriteHeaderTotalCell();

            columnIndex++;

            //WriteHeaderTotalDaysCell();

            columnIndex = 1;

            rowIndex++;
        }

        private void WriteTimesheetColName(Col col)
        {
            //RangeUnion(worksheetIndex, 1, rowIndex, 5, rowIndex);
            RangeSetFontBold(worksheetIndex, columnIndex, rowIndex, columnIndex + 2, rowIndex);
            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "COGNOME", ExcelInsertTypeEnum.Content);
            RangeSetFontColor(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, Color.Red);
            CellInsertValue(worksheetIndex, columnIndex, rowIndex + 1, col.Cognome_Col, ExcelInsertTypeEnum.Content);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 20);
            columnIndex++;

            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "NOME", ExcelInsertTypeEnum.Content);
            RangeSetFontColor(worksheetIndex, columnIndex, rowIndex, columnIndex, rowIndex, Color.Red);
            CellInsertValue(worksheetIndex, columnIndex, rowIndex + 1, col.Nome_Col, ExcelInsertTypeEnum.Content);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 15);
            columnIndex++;

            CellInsertValue(worksheetIndex, columnIndex, rowIndex, "CENTRO DI COSTO", ExcelInsertTypeEnum.Content);
            RangeSetFontColor(worksheetIndex, columnIndex, rowIndex,columnIndex,rowIndex,Color.Red);
            RangeSetBackgroundColor(worksheetIndex, 1,rowIndex,3,rowIndex,Color.Yellow, fillStyle);
            ColumnsSetWidth(worksheetIndex, columnIndex, columnIndex, 30);
        }

        private void WriteTimesheetHeader()
        {
            RangeUnion(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            CellInsertValue(worksheetIndex, 1, 1, ExportDate.ToString("MMMM yyyy").ToUpper(), ExcelInsertTypeEnum.Content);
            RangeSetFontBold(worksheetIndex, 1, rowIndex, 34, rowIndex + 4);
            RangeSetTextVerticalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelVerticalAlignment.Center);
            RangeSetTextHorizontalAlignment(worksheetIndex, 1, rowIndex, 34, rowIndex + 4, ExcelHorizontalAlignment.Center);

            rowIndex += 5;
        }

        private void WriteColTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["justification"])
            {
                var justificationDec = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDec))
                    justificationDec = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDec).Decodifica_Tab;

                justificationDec = justificationDec.ToUpper();

                CellInsertValue(worksheetIndex, 3, rowIndex, justificationDec, ExcelInsertTypeEnum.Content);

                double totaleMensile = 0;
                TimeSpan totaleOrario = new TimeSpan(0,0,0);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);
                    double minuti = 0;
                    string valueToPrint = "";
                    if (baseDuration > 0)
                    {
                        double totaleGiornaliero = baseDuration;
                        minuti = timeDuration.TotalMinutes;
                        if (totaleGiornaliero % 1 > 0.9)
                        {
                            double tmp = 1 - totaleGiornaliero % 1;
                            totaleGiornaliero += tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.75)
                        {
                            double tmp = totaleGiornaliero % 1 - 0.75;
                            totaleGiornaliero -= tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.65)
                        {
                            double tmp = 0.75 - totaleGiornaliero % 1;
                            totaleGiornaliero += tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.5)
                        {
                            double tmp = totaleGiornaliero % 1 - 0.5;
                            totaleGiornaliero -= tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.4)
                        {
                            double tmp = 0.5 - totaleGiornaliero % 1;
                            totaleGiornaliero += tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.25)
                        {
                            double tmp = totaleGiornaliero % 1 - 0.25;
                            totaleGiornaliero -= tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.15)
                        {
                            double tmp = 0.25 - totaleGiornaliero % 1;
                            totaleGiornaliero += tmp;
                        }
                        else if (totaleGiornaliero % 1 > 0.0)
                        {
                            double tmp = totaleGiornaliero % 1;
                            totaleGiornaliero -= tmp;
                        }
                        totaleOrario.Add(timeDuration);
                        valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)(totaleGiornaliero * 60));
                        totaleMensile += totaleGiornaliero;

                    } else if (justificationDec == "TOTALE") {
                        if (baseDuration == 0)
                        {
                            valueToPrint = "0";
                        }
                        else {
                            valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)timeDuration.TotalMinutes);
                        }
                    }
                    RangeSetBorders(worksheetIndex, columnIndex, rowIndex, columnIndex + day.Day + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, columnIndex + day.Day + 2, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                    if (justificationDec == "TOTALE") 
                    {
                        string rangeFormula1 = $"{ColumnIndexToNameConversion(columnIndex + day.Day + 2)}{rowIndex - (cartellini["justification"].Count - 1)}:" +
                                $"{ColumnIndexToNameConversion(columnIndex + day.Day + 2)}{rowIndex - 1}";

                        //Formula automatica per la somma dei valori della riga
                        string formula1 = $"=SUM({rangeFormula1})";
                        CellInsertValue(worksheetIndex, columnIndex + day.Day + 2, rowIndex, formula1, ExcelInsertTypeEnum.Formula);
                    }
                }

                string totalHours = "0";
                var timeDurationTotale = TimeSpan.FromHours(justification.TotalHours);
                if (totaleMensile > 0) {
                    totalHours = FromTotalMinutesToFormattedTypeVirgola((int)(totaleMensile * 60));
                }

                string rangeFormula = $"{ColumnIndexToNameConversion(4)}{rowIndex}:" +
                                $"{ColumnIndexToNameConversion(CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3)}{rowIndex}";

                //Formula automatica per la somma dei valori della riga
                string formula = $"=SUM({rangeFormula})";

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, totalHours, ExcelInsertTypeEnum.Content);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 4, rowIndex, formula, ExcelInsertTypeEnum.Formula);

                //RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                //CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 5, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);

                rowIndex++;

            }

        }

        private void WriteStrTimesheetTitle()
        {
            CellInsertValue(worksheetIndex, 1, rowIndex, "SUDDIVISIONE ORE", ExcelInsertTypeEnum.Content);
            RangeSetFontUnderline(worksheetIndex, 1, rowIndex, 1, rowIndex);

            rowIndex += 2;
        }

        private void WriteStrTimesheet(Dictionary<string, List<TimesheetModuleItem>> cartellini)
        {
            foreach (var justification in cartellini["straordinari"])
            {
                var justificationDesc = justification.Justification;

                if (RepoManager.Tab_DecodRepo.ExistParametrized("DECOD_TAB", "MOTIVAZIONI", justificationDesc))
                    justificationDesc = RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB", "MOTIVAZIONI", justificationDesc).Decodifica_Tab;

                justificationDesc = justificationDesc.ToUpper();

                CellInsertValue(worksheetIndex, 1, rowIndex, justificationDesc, ExcelInsertTypeEnum.Content);

                foreach (var day in CommonService.GetDatesFromPeriod(startMonth, endMonth))
                {
                    var baseDuration = (double)justification["Day" + day.Day.ToString("00")];
                    var timeDuration = TimeSpan.FromHours(baseDuration);

                    string valueToPrint = "";
                    if (timeDuration.TotalMinutes > 0) { 
                        valueToPrint = FromTotalMinutesToFormattedTypeVirgola((int)timeDuration.TotalMinutes);
                    } 
                    RangeSetBorders(worksheetIndex, columnIndex + day.Day, rowIndex, columnIndex + day.Day, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                    CellInsertValue(worksheetIndex, columnIndex + day.Day, rowIndex, valueToPrint, ExcelInsertTypeEnum.Content);
                }

                string totalHours = FromTotalMinutesToFormattedTypeVirgola(justification.TotalMinutes);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 2, rowIndex, totalHours, ExcelInsertTypeEnum.Content);

                RangeSetBorders(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
                CellInsertValue(worksheetIndex, CommonService.GetDatesFromPeriod(startMonth, endMonth).Count + 3, rowIndex, justification.TotalDays, ExcelInsertTypeEnum.Content);


                rowIndex++;

            }

        }

        private void WriteHeaderTotalDaysCell()
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Tot. Giorni", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Yellow, fillStyle);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);
            RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

        }

        #region Header

        private void WriteHeaderDayCell(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Yellow, fillStyle);
            RangeSetWrapText(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, true);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1, 6);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);
            RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
        }
        private void WriteHeaderDayCellDay(DateTime date)
        {
            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, date.Day, ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetValueFormat(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, "0");
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 2, rowIndex, Color.LightGray, fillStyle);
            RangeSetWrapText(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, true);
            ColumnsSetWidth(worksheetIndex, columnIndex + 1, columnIndex + 1, 6);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);
            RangeSetFontSize(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, 12);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                //Se giorno festivo
                RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);
            }


        }

        private void WriteHeaderTotalCell()
        {

            CellInsertValue(worksheetIndex, columnIndex + 1, rowIndex, "Totale", ExcelInsertTypeEnum.Content);
            RangeSetBorders(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle, borderColor, borderStyle);
            RangeSetBackgroundColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Yellow, fillStyle);
            RangeSetFontBold(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex);
            RangeSetFontColor(worksheetIndex, columnIndex + 1, rowIndex, columnIndex + 1, rowIndex, Color.Red);

        }

        #endregion

        #endregion
    }
}
