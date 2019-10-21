using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.CartellinoService.Classes.Riga;
using Business.BusinessClasses.Printable.Interfaces.Row;
using Business.BusinessClasses.Printable.Classes.Row;
using Business.BusinessClasses.Printable.Classes.Cell;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Business.BusinessServices.TimesheetService.Abstracts;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Classes.Cell;

namespace Business.BusinessServices.CartellinoService.Classes.Cartellino
{
    internal sealed class ColTimesheetDividedByCant : TimesheetBase<Col>, ITimesheet
    {
        #region CAMPI PRIVATI

        ILookup<CantiereConDescrizione, CartellinoRegV> _registrations;
        ITimesheetDataProvider _registrationsProvider;
        #endregion

        #region CTOR

        public ColTimesheetDividedByCant(TimesheetConfig<Col> config, ITimesheetDataProvider registrationsProvider) : base(config)
        {
            _registrationsProvider = registrationsProvider;
        }

        #endregion

        #region METODI PUBBLICI

        public void BeforeCompute()
        {
            _registrations = _registrationsProvider.GetTimesheetRegistrations(BaseEntity, MinDate, MaxDate).RaggruppatePerCantiere();
        }

        public void Compute()
        {
            GeneraRigaOreLavorate();
            GeneraRigaOreMotivate();
            GeneraRigaOreViaggi();
            GeneraRigaArrotondamenti();
            GeneraRigaTotale();
        }

        public void AfterCompute()
        {
            CalculateWeekendTotals();
        }

        #region STAMPA CARTELLINO

        public IEnumerable<IPrintableRow> GetHeaderRows()
        {
            #region CODICE RIGA 

            var infoRow = new PrintableRow();

            string labelCollaboratore = $"{BaseEntity.CognomeNome_Col}";

            infoRow.Add(new PrintableCell(labelCollaboratore, Color.Black, Color.Transparent));

            #endregion

            #region GIORNI RIGA 

            var headerRow = new PrintableRow();

            int weekNumber = 1;

            headerRow.Add(null);

            foreach (var data in CommonService.EachDay(MinDate, MaxDate))
            {
                Color textColor = Color.Black;

                if (BusinessService.IsDayOff(data))
                    textColor = Color.Red;

                headerRow.Add(new PrintableCell(data.Day, textColor, Color.LightGray));

                if (data.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                {
                    headerRow.Add(new PrintableCell($"Sett. {weekNumber++}", Color.Black, Color.LightGray));
                }

            }

            #endregion

            #region TOTALE RIGA

            if (Parameters.Totale_Prima_Colonna)
            {
                headerRow.InsertAt(1, new PrintableCell("TOTALE", Color.Black, Color.LightGray));
            }
            else
            {
                headerRow.Add(new PrintableCell("TOTALE", Color.Black, Color.LightGray));
            }

            #endregion

            return new List<IPrintableRow>() { infoRow, headerRow };
        }

        public IEnumerable<IPrintableRow> GetBodyRows()
        {
            List<IPrintableRow> bodyRows = new List<IPrintableRow>();

            var groupedRows = TimesheetRows.VisibleRows.GroupBy(timesheetRow => timesheetRow.RowGroupedEntityDescription).OrderBy(group => group.Key).ToList();

            foreach (var gruppo in groupedRows)
            {
                #region RIGA DI RAGGRUPPAMENTO

                var groupRow = new MasterGroupRow();

                groupRow.Add(new PrintableCell(gruppo.Key, Color.Black, Color.White));

                bodyRows.Add(groupRow);

                #endregion

                foreach (var rigaCartellino in gruppo)
                {

                    #region CODICE RIGA 

                    var bodyRow = new PrintableRow(true);

                    string labelRiga = rigaCartellino.RowDescription;

                    bodyRow.Add(new PrintableCell(labelRiga, Color.Black, Color.White));

                    #endregion

                    #region GIORNI RIGA 

                    foreach (var giorno in rigaCartellino)
                    {
                        string durataFormattata = CartellinoHelper.FormatDuration(giorno.Total);

                        bodyRow.Add(new PrintableCell(durataFormattata, Color.Black, Color.White));

                        if (giorno.Date.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                        {
                            var totaleSettimana = rigaCartellino.WeekendTotals.First(g => g.Date == giorno.Date).Total;

                            string totaleSettimanaFormattato = CartellinoHelper.FormatDuration(totaleSettimana);

                            bodyRow.Add(new PrintableCell(totaleSettimanaFormattato, Color.Black, Color.White));
                        }

                    }

                    #endregion

                    #region TOTALE RIGA

                    string totaleFormattato = CartellinoHelper.FormatDuration(rigaCartellino.RowTotal);

                    if (Parameters.Totale_Prima_Colonna)
                    {
                        bodyRow.InsertAt(1, new PrintableCell(totaleFormattato, Color.Black, Color.White));
                    }
                    else
                    {
                        bodyRow.Add(new PrintableCell(totaleFormattato, Color.Black, Color.White));
                    }

                    #endregion

                    bodyRows.Add(bodyRow);
                }
            }

            return bodyRows;
        }

        public IEnumerable<IPrintableRow> GetFooterRows()
        {
            return Enumerable.Empty<IPrintableRow>();
        }

        #endregion

        #endregion

        #region METODI PRIVATI

        #region GENERAZIONE RIGHE

        void GeneraRigaOreLavorate()
        {
            foreach (var registrazioniDelSingoloCantiere in _registrations)
            {
                var cantiere = registrazioniDelSingoloCantiere.Key;

                var oreLavorate = registrazioniDelSingoloCantiere.OreLavorate().PerGiornoConDurata();

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = true;
                rowConfig.RowCode = CHIAVE_ORE_LAVORATE;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);
                rowConfig.RowGroupedEntityId = cantiere.Cant_Id;
                rowConfig.RowGroupedEntityDescription = cantiere.Cant_Mnemonic;

                TimesheetPlanHoursRow riga = new TimesheetPlanHoursRow(rowConfig);

                FillTimesheetRowAndAdd(oreLavorate, riga);

            }


        }
        void GeneraRigaOreMotivate()
        {
            foreach (var registrazioniDelSingoloCantiere in _registrations)
            {
                var cantiere = registrazioniDelSingoloCantiere.Key;

                var oreMotivate = registrazioniDelSingoloCantiere.OreMotivate().PerMotivazionePerGiornoConDurata();

                foreach (var gruppoMotivazione in oreMotivate)
                {
                    var rowConfig = new TimesheetRowConfiguration();

                    rowConfig.MinDate = MinSummaryDate;
                    rowConfig.MaxDate = MaxSummaryDate;
                    rowConfig.IsToShow = Parameters.Visualizza_Motivazioni;
                    rowConfig.RowCode = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Chiave_Tab;
                    rowConfig.RowDescription = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Decodifica_Tab;
                    rowConfig.RowGroupedEntityId = cantiere.Cant_Id;
                    rowConfig.RowGroupedEntityDescription = cantiere.Cant_Mnemonic;

                    TimesheetMotivatedHoursRow riga = new TimesheetMotivatedHoursRow(rowConfig);

                    FillTimesheetRowAndAdd(gruppoMotivazione.RegistrazioniPerDataConDurata, riga);
                }
            }
        }
        void GeneraRigaOreViaggi()
        {


        }
        void GeneraRigaDelta()
        {


        }
        void GeneraRigaArrotondamenti()
        {

        }
        void GeneraRigaTotale()
        {
            var righeDaTotalizzare = TimesheetRows.SummableRows.GroupBy(entità => new CantiereConDescrizione { Cant_Id = (int)entità.RowGroupedEntityId, Cant_Mnemonic = entità.RowGroupedEntityDescription }).ToList();

            var days = CommonService.EachDay(MinDate, MaxDate).ToList();

            foreach (var righeDaTotalizzareDelSingoloCantiere in righeDaTotalizzare)
            {
                int cellIndex = days.Count() - 1;

                var cantiere = righeDaTotalizzareDelSingoloCantiere.Key;

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = Parameters.Visualizza_Totale;
                rowConfig.RowCode = CHIAVE_ORE_TOTALE;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE);
                rowConfig.RowGroupedEntityId = cantiere.Cant_Id;
                rowConfig.RowGroupedEntityDescription = cantiere.Cant_Mnemonic;

                TimesheetTotalRow rigaTotale = new TimesheetTotalRow(rowConfig);

                //rigaTotale.RowGroupedEntityDescription = cantiere.Cant_Mnemonic;

                for (int dayIndex = 0; dayIndex < cellIndex; dayIndex++)
                {
                    TimeSpan totaleDelGiorno = TimeSpan.Zero;

                    foreach (var riga in righeDaTotalizzareDelSingoloCantiere)
                    {
                        totaleDelGiorno = totaleDelGiorno.Add(riga[dayIndex].Total);
                    }

                    var cella = new TimesheetCell { Date = days[dayIndex], Total = totaleDelGiorno };

                    rigaTotale.Add(cella);

                }

                TimesheetRows.AddRow(rigaTotale);
            }
        }

    }

    #endregion

    #endregion

}

