
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.BusinessClasses.CartellinoServiceDTOs;
using Domain;
using Business.BusinessClasses.Printable.Interfaces.Row;
using Business.BusinessClasses.Printable.Classes.Row;
using Business.BusinessClasses.Printable.Classes.Cell;
using System.Drawing;
using Business.BusinessServices.TimesheetService.Abstracts;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.TimesheetService.Classes.Row;
using Business.BusinessServices.CartellinoService.Classes.Riga;
using Business.BusinessServices.TimesheetService.Classes.Cell;

namespace Business.BusinessServices.TimesheetService.Classes.Timesheet
{
    internal sealed class CantTimesheet : TimesheetBase<Cant>, ITimesheet
    {
        ITimesheetDataProvider _registrationsProvider;
        IEnumerable<CartellinoRegV> _registrations;

        #region CTOR

        public CantTimesheet(TimesheetConfig<Cant> config, ITimesheetDataProvider registrationsProvider) : base(config)
        {
            _registrationsProvider = registrationsProvider;
        }

        #endregion

        #region METODI PUBBLICI

        public void BeforeCompute()
        {
            _registrations = _registrationsProvider.GetTimesheetRegistrations(BaseEntity, MinDate, MaxDate);
        }

        public void Compute()
        {
            GeneraRigaOrario();
            GeneraRigaOreLavorate();
            GeneraRigaOreMotivate();
            GeneraRigaOreViaggi();
            GeneraRigaArrotondamenti();
            GeneraRigaDelta();
            GeneraRigaTotale();
        }

        public void AfterCompute()
        {
            CalculateWeekendTotals();
        }

        #region STAMPA XLSX

        public IEnumerable<IPrintableRow> GetHeaderRows()
        {
            IPrintableRow infoRow = CreateInfoRow();
            IPrintableRow tableHeaderRow = CreateTableHeaderRow();

            return new List<IPrintableRow>() { infoRow, tableHeaderRow };

        }

        public IEnumerable<IPrintableRow> GetBodyRows()
        {
            List<IPrintableRow> bodyRows = new List<IPrintableRow>();

            foreach (var rigaCartellino in TimesheetRows.VisibleRows)
            {
                var bodyRow = new PrintableRow();

                #region CODICE RIGA 

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

        void GeneraRigaOrario()
        {
            IDictionary<DateTime, TimeSpan> planDurationByDay = GetRangePlanDuration(BaseEntity, MinDate, MaxDate);

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Piano;
            rowConfig.RowCode = CHIAVE_ORE_PIANO;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);

            TimesheetWorkedHoursRow planTimesheetRow = new TimesheetWorkedHoursRow(rowConfig);

            FillTimesheetRowAndAdd(planDurationByDay, planTimesheetRow);
        }
        void GeneraRigaOreLavorate()
        {
            var orePerGiornoConDurata = _registrations.OreLavorate().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Ore;
            rowConfig.RowCode = CHIAVE_ORE_LAVORATE;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);

            TimesheetWorkedHoursRow riga = new TimesheetWorkedHoursRow(rowConfig);

            FillTimesheetRowAndAdd(orePerGiornoConDurata, riga);
        }
        void GeneraRigaOreMotivate()
        {
            var oreMotivate = _registrations.OreMotivate().PerMotivazionePerGiornoConDurata();

            foreach (var gruppoMotivazione in oreMotivate)
            {
                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = Parameters.Visualizza_Motivazioni;
                rowConfig.RowCode = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Chiave_Tab;
                rowConfig.RowDescription = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Decodifica_Tab;

                TimesheetMotivatedHoursRow riga = new TimesheetMotivatedHoursRow(rowConfig);

                FillTimesheetRowAndAdd(gruppoMotivazione.RegistrazioniPerDataConDurata, riga);
            }
        }
        void GeneraRigaOreViaggi()
        {
            var oreViaggi = _registrations.OreViaggi().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Viaggi;
            rowConfig.RowCode = CHIAVE_ORE_VIAGGI;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI);

            TimesheetTripHoursRow riga = new TimesheetTripHoursRow(rowConfig);

            FillTimesheetRowAndAdd(oreViaggi, riga);

        }
        void GeneraRigaDelta()
        {
            var rigaPianoOrario = TimesheetRows.GetRows(TimesheetRowType.Orario);

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Delta;
            rowConfig.RowCode = CHIAVE_ORE_DELTA;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA);

            if (!rigaPianoOrario.Any())
            {
                TimesheetRows.AddRow(TimesheetDeltaRow.Default(rowConfig));
                return;
            }

            var oreDaConteggiare = TimesheetRows.SummableRows;

            TimesheetDeltaRow rigaDelta = new TimesheetDeltaRow(rowConfig);

            int cellIndex = 0;

            foreach (var timesheetPlanRow in rigaPianoOrario)
            {
                foreach (var timesheetRow in oreDaConteggiare)
                {
                    TimeSpan durataTemporanea = TimeSpan.Zero;

                    var timesheetCell = timesheetRow.ElementAt(cellIndex);

                    durataTemporanea = durataTemporanea.Add(timesheetCell.Total);

                    rigaDelta.Add(new TimesheetCell() { Date = timesheetCell.Date, Total = durataTemporanea.Subtract(timesheetCell.Total) });
                }

                cellIndex++;
            }

            TimesheetRows.AddRow(rigaDelta);
        }
        void GeneraRigaArrotondamenti()
        {
            var oreArrotondamenti = _registrations.OreArrotondamenti().PerGiornoConDurata();

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = true;
            rowConfig.RowCode = CHIAVE_ORE_ARROTONDAMENTI;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT_DURATA);

            TimesheetRoundingHoursRow riga = new TimesheetRoundingHoursRow(rowConfig);

            FillTimesheetRowAndAdd(oreArrotondamenti, riga);
        }
        void GeneraRigaTotale()
        {
            var righeDaTotalizzare = TimesheetRows.SummableRows;

            var days = CommonService.EachDay(MinDate, MaxDate).ToList();

            int cellIndex = days.Count() - 1;

            var rowConfig = new TimesheetRowConfiguration();

            rowConfig.MinDate = MinSummaryDate;
            rowConfig.MaxDate = MaxSummaryDate;
            rowConfig.IsToShow = Parameters.Visualizza_Totale;
            rowConfig.RowCode = CHIAVE_ORE_TOTALE;
            rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE);

            TimesheetTotalRow rigaTotale = new TimesheetTotalRow(rowConfig);

            for (int dayIndex = 0; dayIndex < cellIndex; dayIndex++)
            {
                TimeSpan totaleDelGiorno = TimeSpan.Zero;

                foreach (var riga in righeDaTotalizzare)
                {
                    totaleDelGiorno = totaleDelGiorno.Add(riga[dayIndex].Total);
                }

                var cella = new TimesheetCell { Date = days[dayIndex], Total = totaleDelGiorno };

                rigaTotale.Add(cella);
            }

            TimesheetRows.AddRow(rigaTotale);
        }

        IDictionary<DateTime, TimeSpan> GetRangePlanDuration(Cant cantiere, DateTime minDate, DateTime maxDate)
        {
            Dictionary<DateTime, TimeSpan> rangePlanDuration = new Dictionary<DateTime, TimeSpan>();

            if (cantiere.Tab_Orari_Tipo == null)
                return rangePlanDuration;

            IEnumerable<Tab_Orari> cantPlans = cantiere.Tab_Orari_Tipo.Tab_Orari.Where(or => or.Data_Inizio <= minDate).ToList();

            DateTime maxPlanDate = cantPlans.Max(or => or.Data_Inizio);

            cantPlans = cantPlans.Where(or => or.Data_Inizio == maxPlanDate).ToList();

            IEnumerable<DateTime> daysToCheck = CommonService.EachDay(minDate, maxDate).ToList();

            foreach (DateTime day in daysToCheck)
            {
                TimeSpan planDayDuration = TimeSpan.Zero;

                foreach (Tab_Orari cantPlan in cantPlans)
                {
                    if (BusinessService.IsToApplyPlan(cantPlan, day))
                    {
                        var planMinutes = cantPlan.HasTimeBorders ? (cantPlan.Ora_U.Value.Subtract(cantPlan.Ora_E.Value).TotalMinutes) : Convert.ToDouble(cantPlan.Durata_Minuti);

                        planDayDuration = planDayDuration.Add(TimeSpan.FromMinutes(planMinutes));
                    }
                }

                rangePlanDuration.Add(day, planDayDuration);

            }

            return rangePlanDuration;
        }

        #endregion

        private IPrintableRow CreateInfoRow()
        {
            var infoRow = new PrintableRow();
            var descrizioneCantiere = $"{BaseEntity.Codice_Cantiere} {BaseEntity.Descrizione_Can}";
            var cella = new PrintableCell(descrizioneCantiere, Color.Black, Color.Transparent);
            infoRow.Add(cella);
            return infoRow;
        }
        private IPrintableRow CreateTableHeaderRow()
        {
            var headerRow = new PrintableRow();

            int weekNumber = 1;

            foreach (var date in CommonService.EachDay(MinDate, MaxDate))
            {
                Color textColor = Color.Black;

                if (BusinessService.IsDayOff(date))
                    textColor = Color.Red;

                var cell = new PrintableCell(date.Day, textColor, Color.LightGray);

                headerRow.Add(cell);

                if (date.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                {
                    cell = new PrintableCell($"Sett. {weekNumber++}", Color.Black, Color.LightGray);

                    headerRow.Add(cell);
                }
            }

            if (Parameters.Totale_Prima_Colonna)
                headerRow.InsertAt(1, new PrintableCell("TOTALE", Color.Black, Color.LightGray));
            else
                headerRow.Add(new PrintableCell("TOTALE", Color.Black, Color.LightGray));


            return headerRow;
        }



        #endregion
    }
}
