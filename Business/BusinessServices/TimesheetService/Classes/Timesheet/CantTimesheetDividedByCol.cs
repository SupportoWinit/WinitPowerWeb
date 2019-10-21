using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.CartellinoService.Classes.Riga;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Business.BusinessClasses.Printable.Interfaces.Row;
using Business.BusinessClasses.Printable.Classes.Row;
using Business.BusinessClasses.Printable.Classes.Cell;
using System.Drawing;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.BusinessServices.TimesheetService.Classes.RowConfig;
using Business.BusinessServices.TimesheetService.Abstracts;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Classes.Cell;

namespace Business.BusinessServices.CartellinoService.Classes.Cartellino
{
    internal sealed class CantTimesheetDividedByCol : TimesheetBase<Cant>, ITimesheet
    {
        #region CAMPI PRIVATI

        ITimesheetDataProvider _registrationsProvider;
        ILookup<CollaboratoreConDescrizione, CartellinoRegV> _registrazioni;

        #endregion

        #region CTOR

        public CantTimesheetDividedByCol(TimesheetConfig<Cant> config, ITimesheetDataProvider registrationsProvider) : base(config)
        {
            _registrationsProvider = registrationsProvider;
        }

        #endregion

        #region METODI PUBBLICI

        public void BeforeCompute()
        {
            _registrazioni = _registrationsProvider.GetTimesheetRegistrations(BaseEntity, MinDate, MaxDate).RaggruppatePerCollaboratore();
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

        public IEnumerable<IPrintableRow> GetHeaderRows()
        {

            #region CODICE RIGA 

            var infoRow = new PrintableRow();

            string labelCantiere = $"{BaseEntity.Codice_Cantiere} {BaseEntity.Descrizione_Can}";

            infoRow.Add(new PrintableCell(labelCantiere, Color.Black, Color.LightGreen));

            #endregion

            #region GIORNI RIGA 

            var headerRow = new PrintableRow();

            headerRow.Add(PrintableCell.Empty); //Prima cella vuota

            int weekNumber = 1;

            foreach (var data in CommonService.EachDay(MinDate, MaxDate))
            {
                Color textColor = Color.Black;

                if (BusinessService.IsDayOff(data))
                    textColor = Color.Red;

                headerRow.Add(new PrintableCell(data.Day, textColor, Color.LightGray));

                if (data.DayOfWeek == DayOfWeek.Sunday && Parameters.Visualizza_Totali_Settimanali)
                {
                    headerRow.Add(new PrintableCell($"Sett. {weekNumber++}", Color.Black, Color.LightBlue));
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

            var groupedRows = TimesheetRows.VisibleRows.GroupBy(riga => riga.RowGroupedEntityDescription).OrderBy(group => group.Key).ToList();

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

                            bodyRow.Add(new PrintableCell(totaleSettimanaFormattato, Color.Black, Color.LightGray));
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

        #region METODI PRIVATI

        #region GENERAZIONE RIGHE

        void GeneraRigaOreLavorate()
        {
            foreach (var registrazioniDelSingoloCollaboratore in _registrazioni)
            {
                var collaboratore = registrazioniDelSingoloCollaboratore.Key;

                var oreLavorate = registrazioniDelSingoloCollaboratore.OreLavorate().PerGiornoConDurata();

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = Parameters.Visualizza_Ore;
                rowConfig.RowCode = CHIAVE_ORE_LAVORATE;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE_LAVORATE);
                rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                rowConfig.RowGroupedEntityDescription = collaboratore.Col_Desc;

                TimesheetWorkedHoursRow riga = new TimesheetWorkedHoursRow(rowConfig);

                //riga.DecodificaEntitàRaggruppata = collaboratore.Col_Desc;

                FillTimesheetRowAndAdd(oreLavorate, riga);
            }


        }
        void GeneraRigaOreMotivate()
        {
            foreach (var registrazioniDelSingoloCollaboratore in _registrazioni)
            {
                var collaboratore = registrazioniDelSingoloCollaboratore.Key;

                var oreMotivate = registrazioniDelSingoloCollaboratore.OreMotivate().PerMotivazionePerGiornoConDurata();

                foreach (var gruppoMotivazione in oreMotivate)
                {
                    var rowConfig = new TimesheetRowConfiguration();

                    rowConfig.MinDate = MinSummaryDate;
                    rowConfig.MaxDate = MaxSummaryDate;
                    rowConfig.IsToShow = Parameters.Visualizza_Ore;
                    rowConfig.RowCode = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Chiave_Tab;
                    rowConfig.RowDescription = CartellinoHelper.Motivazioni[gruppoMotivazione.Motivation_Id].Decodifica_Tab;
                    rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                    rowConfig.RowGroupedEntityDescription = collaboratore.Col_Desc;

                    TimesheetMotivatedHoursRow riga = new TimesheetMotivatedHoursRow(rowConfig);

                    FillTimesheetRowAndAdd(gruppoMotivazione.RegistrazioniPerDataConDurata, riga);
                }
            }
        }
        void GeneraRigaArrotondamenti()
        {
            foreach (var registrazioniDelSingoloCollaboratore in _registrazioni)
            {
                var collaboratore = registrazioniDelSingoloCollaboratore.Key;

                var oreArrotondamenti = registrazioniDelSingoloCollaboratore.OreArrotondamenti().PerGiornoConDurata();

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = true;
                rowConfig.RowCode = CHIAVE_ORE_ARROTONDAMENTI;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT_DURATA);
                rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                rowConfig.RowGroupedEntityDescription = collaboratore.Col_Desc;

                TimesheetRoundingHoursRow riga = new TimesheetRoundingHoursRow(rowConfig);

                //riga.DecodificaEntitàRaggruppata = collaboratore.Col_Desc;

                FillTimesheetRowAndAdd(oreArrotondamenti, riga);
            }
        }
        void GeneraRigaDelta() { }
        void GeneraRigaOrario() { }
        void GeneraRigaOreViaggi()
        {
            foreach (var registrazioniDelSingoloCollaboratore in _registrazioni)
            {
                var collaboratore = registrazioniDelSingoloCollaboratore.Key;

                var oreViaggi = registrazioniDelSingoloCollaboratore.OreViaggi().PerGiornoConDurata();

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = true;
                rowConfig.RowCode = CHIAVE_ORE_ARROTONDAMENTI;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_VIAGGI);
                rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                rowConfig.RowGroupedEntityDescription = collaboratore.Col_Desc;

                TimesheetTripHoursRow riga = new TimesheetTripHoursRow(rowConfig);

                //riga.RowGroupedEntityDescription = collaboratore.Col_Desc;

                FillTimesheetRowAndAdd(oreViaggi, riga);
            }
        }
        void GeneraRigaTotale()
        {
            var righeDaTotalizzare = TimesheetRows.SummableRows.GroupBy(entità => new CollaboratoreConDescrizione { Col_Id = (int)entità.RowGroupedEntityId, Col_Desc = entità.RowGroupedEntityDescription }).ToList();

            var days = CommonService.EachDay(MinDate, MaxDate).ToList();

            foreach (var righeDaTotalizzareDelSingoloCollaboratore in righeDaTotalizzare)
            {
                int cellIndex = days.Count() - 1;

                var collaboratore = righeDaTotalizzareDelSingoloCollaboratore.Key;

                var rowConfig = new TimesheetRowConfiguration();

                rowConfig.MinDate = MinSummaryDate;
                rowConfig.MaxDate = MaxSummaryDate;
                rowConfig.IsToShow = true;
                rowConfig.RowCode = CHIAVE_ORE_TOTALE;
                rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                rowConfig.RowDescription = BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE);
                rowConfig.RowGroupedEntityId = collaboratore.Col_Id;
                rowConfig.RowGroupedEntityDescription = collaboratore.Col_Desc;

                TimesheetTotalRow rigaTotale = new TimesheetTotalRow(rowConfig);
                
                for (int dayIndex = 0; dayIndex < cellIndex; dayIndex++)
                {
                    TimeSpan totaleDelGiorno = TimeSpan.Zero;

                    foreach (var riga in righeDaTotalizzareDelSingoloCollaboratore)
                    {
                        totaleDelGiorno = totaleDelGiorno.Add(riga[dayIndex].Total);
                    }

                    var cella = new TimesheetCell { Date = days[dayIndex], Total = totaleDelGiorno };

                    rigaTotale.Add(cella);

                }

                TimesheetRows.AddRow(rigaTotale);
            }
        }
        
        #endregion

        #endregion
    }
}
