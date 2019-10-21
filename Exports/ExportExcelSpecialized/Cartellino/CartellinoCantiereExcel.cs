using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Business.Repository;
using Common;
using Domain;
using Business.ExportExcelEngine;
using Business.BusinessExtension;

namespace Exports.ExportExcelSpecialized.Cartellino
{

    /*
     *                              In questa classe viene creato un albero a 5 livelli
     * 
     *                                                     O                             Livello 0: creazione foglio e intestazione
     *                                                    / \
     *                                                   /   \
     *                                                  O     O                          Livello 1: collaboratore
     *                                                 /       \
     *                                                /         \   
     *                                               O           O                       Livello 2: header dell'orario
     *                                              /             \
     *                                             /               \
     *                                            O                 O                    Livello 3: timeSheet orario
     *                                           / \               / \
     *                                          /   \             /   \
     *                                         O     O           O     O                 Livello 4: cantieri(Descrizioni)
     *                                        /     / \         / \     \
     *                                       /     /   \       /   \     \
     *                                      O     O     O     O     O     O              Livello 5: cartellini per motivazioni
     * 
     *                               Per ogni cantiere vengono stampati i relativi timesheet
     */


    public class CartellinoCantiereExcel : IExportExcelSpecialized<TimesheetModuleItem>
    {
        private TimesheetModuleItem cart = null;
        private Cant cant = null;
        private Col col = null;
        private List<string> days;
        private DateTime expDate;
        public ExportExcelTreeNode GetRootTreeNode(List<TimesheetModuleItem> items)
        {            //Col             //cant

            //Viene organizzato un dizionario collaboratore -> cantieri per utilizzarlo nella costruzione dell'albero
            Dictionary<int, Dictionary<int, List<TimesheetModuleItem>>> collaboratori = new Dictionary<int, Dictionary<int, List<TimesheetModuleItem>>>();
            
            foreach(var t in items)
            {
                if (!collaboratori.ContainsKey(t.ColId))
                {
                    collaboratori[t.ColId] = new Dictionary<int, List<TimesheetModuleItem>>();

                }
                if (!collaboratori[t.ColId].ContainsKey(t.CantId))
                {
                    collaboratori[t.ColId][t.CantId] = new List<TimesheetModuleItem>();
                }
                
                collaboratori[t.ColId][t.CantId].Add(t);
                
            }

            //
            //Creazione della lista dei giorni contenuti nel mese indicato per la riga excel dei giorni
            days = new List<string>();

            expDate = items.ElementAt(0).StartDate;
            DateTime day = items.ElementAt(0).StartDate;

            int giorniM = DateTime.DaysInMonth(day.Year, day.Month);

            for (int d = 1; d <= giorniM; d++)
            {
                DayOfWeek giornoIta = day.DayOfWeek;
                switch (giornoIta.ToString())
                {
                    case "Monday":
                        days.Add("Lun " + d);
                        break;
                    case "Tuesday":
                        days.Add("Mar " + d);
                        break;
                    case "Wednesday":
                        days.Add("Mer " + d);
                        break;
                    case "Thursday":
                        days.Add("Gio " + d);
                        break;
                    case "Friday":
                        days.Add("Ven " + d);
                        break;
                    case "Saturday":
                        days.Add("Sab " + d);
                        break;
                    case "Sunday":
                        days.Add("Dom " + d);
                        break;
                }
                day = day.AddDays(1);


            }
            

            ExportExcelTreeNode root = new ExportExcelTreeNode();
            
            List<ExportExcelTreeNode> cols = new List<ExportExcelTreeNode>();    //Lista di collaboratori (padre = root)
            List<ExportExcelTreeNode> cantieri =null;                            //Lista cantieri (padre = pianoOrario)
            List<ExportExcelTreeNode> timesheets;                                //Lista timeSheets  (padre = cantieri[i])
            ExportExcelTreeNode pianoOrario = null;                                 
            ExportExcelTreeNode int_piano = null;


            foreach (var col in collaboratori)
            {
                ExportExcelTreeNode coll = new ExportExcelTreeNode(RepoManager.ColRepo.First(c => c.Col_Id == col.Key));
                

                cantieri = new List<ExportExcelTreeNode>();
                foreach (var cants in collaboratori[col.Key])
                {
                    if (cants.Key != 0)
                    {

                        ExportExcelTreeNode cantiere = new ExportExcelTreeNode(RepoManager.CantRepo.First(can => can.Cant_Id == cants.Key));
                        
                        timesheets = new List<ExportExcelTreeNode>();

                        foreach (TimesheetModuleItem time in collaboratori[col.Key][cants.Key])
                        {
                            if (time.Justification.Length <=3)
                            {
                                Tab_Decod newJust = RepoManager.Tab_DecodRepo.FirstOrDefault(t => t.Nome_Tab == "MOTIVAZIONI" && t.Chiave_Tab == time.Justification);
                                time.Justification = (newJust != null) ? newJust.Decodifica_Tab : time.Justification;
                            }
                            ExportExcelTreeNode times = new ExportExcelTreeNode(time);
                            timesheets.Add(times);
                        }
                        cantiere.AddChildren(timesheets);
                        cantieri.Add(cantiere);
                    }
                    else
                    {
                        int_piano = new ExportExcelTreeNode();
                        pianoOrario = new ExportExcelTreeNode(collaboratori[col.Key][cants.Key].ElementAt(0));
                        
                    }  
                }

                pianoOrario.AddChildren(cantieri);
                int_piano.AddChild(pianoOrario);
                coll.AddChild(int_piano);
                cols.Add(coll);
            }
            root.AddChildren(cols);
            
            return root;
        }

        public List<ExportExcelTreeNodeTemplate> GetTreeNodeTemplates()
        {
            List<ExportExcelTreeNodeTemplate> templates = new List<ExportExcelTreeNodeTemplate>();

            //creo il foglio excel
            ExportExcelTreeNodeTemplate createTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateSheet, null, 0);

            createTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Sheet, null, "Registrazioni"));

            templates.Add(createTemplate);

            //Copio la parte di header e inserisco i dati (data creazione)
            ExportExcelTreeNodeTemplate headerTemplate = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateHeader, "1:4", 0);
            headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, "A1", String.Format("Mese: {0}", CommonService.GetMonthName(expDate).ToUpper())));
            headerTemplate.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.StaticCell, "A3", String.Format("Data export: {0}", DateTime.Now)));
            templates.Add(headerTemplate);

            //template che si ripete per ogni riga di dati del foglio

            ExportExcelTreeNodeTemplate collaboratore = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "5:5", 1);
            collaboratore.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => col.CognomeNome_Col)));
            templates.Add(collaboratore);

            
            ExportExcelTreeNodeTemplate headerOrario = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "6:6", 2);
            headerOrario.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, GetColumnName(0), "Tipo"));
            int ind = 0;
            for (int i = 0; i < days.Count; i++)
            {
                ind = i;
                headerOrario.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, GetColumnName(i + 1), days[i]));
            };
            headerOrario.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, GetColumnName(ind + 2), "Totale giorni"));
            headerOrario.Mappings.Add(new ExportExcelTreeNodeStaticMapping(ExportExcelReferenceEnum.Column, GetColumnName(ind + 3), "Totale ore"));
            templates.Add(headerOrario);

            ExportExcelTreeNodeTemplate blank = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateBlankRow,"11:11",2,true);
            templates.Add(blank);
            ExportExcelTreeNodeTemplate orario = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "7:7", 3);
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => cart.Justification)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => cart.Day01)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => cart.Day02)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => cart.Day03)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "E", CommonService.GetPropertyName(() => cart.Day04)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "F", CommonService.GetPropertyName(() => cart.Day05)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "G", CommonService.GetPropertyName(() => cart.Day06)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "H", CommonService.GetPropertyName(() => cart.Day07)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "I", CommonService.GetPropertyName(() => cart.Day08)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "J", CommonService.GetPropertyName(() => cart.Day09)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "K", CommonService.GetPropertyName(() => cart.Day10)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "L", CommonService.GetPropertyName(() => cart.Day11)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "M", CommonService.GetPropertyName(() => cart.Day12)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "N", CommonService.GetPropertyName(() => cart.Day13)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "O", CommonService.GetPropertyName(() => cart.Day14)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "P", CommonService.GetPropertyName(() => cart.Day15)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Q", CommonService.GetPropertyName(() => cart.Day16)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "R", CommonService.GetPropertyName(() => cart.Day17)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "S", CommonService.GetPropertyName(() => cart.Day18)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "T", CommonService.GetPropertyName(() => cart.Day19)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "U", CommonService.GetPropertyName(() => cart.Day20)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "V", CommonService.GetPropertyName(() => cart.Day21)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "W", CommonService.GetPropertyName(() => cart.Day22)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "X", CommonService.GetPropertyName(() => cart.Day23)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Y", CommonService.GetPropertyName(() => cart.Day24)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Z", CommonService.GetPropertyName(() => cart.Day25)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AA", CommonService.GetPropertyName(() => cart.Day26)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AB", CommonService.GetPropertyName(() => cart.Day27)));
            orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AC", CommonService.GetPropertyName(() => cart.Day28)));
            if (ind == 29)
            {
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => cart.Day29)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => cart.Day30)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => cart.TotalDays)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AG", CommonService.GetPropertyName(() => cart.TotalHours)));
            }
            else if(ind == 30) {
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => cart.Day29)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => cart.Day30)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => cart.Day31)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AG", CommonService.GetPropertyName(() => cart.TotalDays)));
                orario.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AH", CommonService.GetPropertyName(() => cart.TotalHours)));
                
            }
            templates.Add(orario);

            ExportExcelTreeNodeTemplate cantiere = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "8:8", 4);
            cantiere.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => cant.Descrizione_Can)));
            templates.Add(cantiere);

            ExportExcelTreeNodeTemplate cartellino = new ExportExcelTreeNodeTemplate(ExportExcelActionEnum.CreateRow, "7:7", 5);
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "A", CommonService.GetPropertyName(() => cart.Justification)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "B", CommonService.GetPropertyName(() => cart.Day01)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "C", CommonService.GetPropertyName(() => cart.Day02)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "D", CommonService.GetPropertyName(() => cart.Day03)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "E", CommonService.GetPropertyName(() => cart.Day04)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "F", CommonService.GetPropertyName(() => cart.Day05)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "G", CommonService.GetPropertyName(() => cart.Day06)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "H", CommonService.GetPropertyName(() => cart.Day07)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "I", CommonService.GetPropertyName(() => cart.Day08)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "J", CommonService.GetPropertyName(() => cart.Day09)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "K", CommonService.GetPropertyName(() => cart.Day10)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "L", CommonService.GetPropertyName(() => cart.Day11)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "M", CommonService.GetPropertyName(() => cart.Day12)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "N", CommonService.GetPropertyName(() => cart.Day13)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "O", CommonService.GetPropertyName(() => cart.Day14)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "P", CommonService.GetPropertyName(() => cart.Day15)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Q", CommonService.GetPropertyName(() => cart.Day16)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "R", CommonService.GetPropertyName(() => cart.Day17)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "S", CommonService.GetPropertyName(() => cart.Day18)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "T", CommonService.GetPropertyName(() => cart.Day19)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "U", CommonService.GetPropertyName(() => cart.Day20)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "V", CommonService.GetPropertyName(() => cart.Day21)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "W", CommonService.GetPropertyName(() => cart.Day22)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "X", CommonService.GetPropertyName(() => cart.Day23)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Y", CommonService.GetPropertyName(() => cart.Day24)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "Z", CommonService.GetPropertyName(() => cart.Day25)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AA", CommonService.GetPropertyName(() => cart.Day26)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AB", CommonService.GetPropertyName(() => cart.Day27)));
            cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AC", CommonService.GetPropertyName(() => cart.Day28)));
            if (ind == 29)
            {
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => cart.Day29)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => cart.Day30)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => cart.TotalDays)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AG", CommonService.GetPropertyName(() => cart.TotalHours)));
            }
            else if (ind == 30)
            {
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AD", CommonService.GetPropertyName(() => cart.Day29)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AE", CommonService.GetPropertyName(() => cart.Day30)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AF", CommonService.GetPropertyName(() => cart.Day31)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AG", CommonService.GetPropertyName(() => cart.TotalDays)));
                cartellino.Mappings.Add(new ExportExcelTreeNodeMapping(ExportExcelReferenceEnum.Column, "AH", CommonService.GetPropertyName(() => cart.TotalHours)));

            }
            templates.Add(cartellino);






            return templates;
        }

        public string GetColumnName(int index){
            const byte BASE = 'Z' - 'A' + 1;
            string name = String.Empty;
            do {
                name = Convert.ToChar('A' + index % BASE) + name;
                index = index / BASE - 1;
            } while (index >= 0);
            return name;
        }

        public IEnumerable<Tuple<string, string, string>> VisibleFields { get; set; }

        public IList<string> GetCellsToMerge()
        {
            return new List<string>();
        }

        public string ModelName { get; set; }

        public bool IsAutoFitColumns
        {
            get { return false; }
        }

        public string RepeatRowsAddress
        {
            get { return string.Empty; }
        }
    }
}
