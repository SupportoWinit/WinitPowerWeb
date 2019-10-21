using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.TimesheetService.Classes.Timesheet;
using Business.BusinessServices.TimesheetService.Classes.Config;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Classes;
using Business.BusinessServices.TimesheetService.Providers.Registrations.Interfaces;
using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.CartellinoService.Classes.Cartellino;

namespace Business.BusinessServices.TimesheetService.Factory
{
    internal sealed class CartellinoFactory
    {
        private ITimesheetDataProvider registrationProvider;

        public CartellinoFactory()
        {
            registrationProvider = new TimesheetDataProvider();


        }

        public ITimesheet Create(int id, DateTime month, TimesheetParams parameters)
        {
            switch (parameters.Cartellino_Base_Type)
            {
                case Common.CartellinoBaseTypeEnum.Cantiere:
                    var cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == id, true);
                    return Create(cantiere, month, parameters);

                case Common.CartellinoBaseTypeEnum.Collaboratore:
                    var collaboratore = RepoManager.ColRepo.First(c => c.Col_Id == id, true);
                    return Create(collaboratore, month, parameters);
            }

            return null;
        }

        public IEnumerable<ITimesheet> Create(IEnumerable<int> ids, DateTime month, TimesheetParams parameters)
        {
            switch (parameters.Cartellino_Base_Type)
            {
                case Common.CartellinoBaseTypeEnum.Cantiere:
                    var cantieri = RepoManager.CantRepo.Find(c => ids.Contains(c.Cant_Id), true);
                    var cartelliniPerCantiere = Cycle(cantieri, (cant) => Create(cant, month, parameters));
                    return cartelliniPerCantiere;
                case Common.CartellinoBaseTypeEnum.Collaboratore:
                    var collaboratori = RepoManager.ColRepo.Find(c => ids.Contains(c.Col_Id), true);
                    var cartelliniPerCollaboratore = Cycle(collaboratori, (col) => Create(col, month, parameters));
                    return cartelliniPerCollaboratore;
            }

            return null;
        }

        public ITimesheet Create(Col collaboratore, DateTime month, TimesheetParams parameters)
        {
            ITimesheet cartellino = null;

            DateTime monthRequested = Common.CommonService.GetFirstMonthDay(month);
           
            TimesheetConfig<Col> config = CreateConfig(collaboratore, monthRequested, parameters);

            switch (parameters.Cartellino_Grouped_Entity_Type)
            {
                case Common.CartellinoGroupedEntityEnum.Cantiere:
                    cartellino = new ColTimesheetDividedByCant(config, registrationProvider);
                    break;
                case Common.CartellinoGroupedEntityEnum.None:
                    cartellino = new ColTimesheet(config, registrationProvider);
                    break;
            }

            cartellino.BeforeCompute();
            cartellino.Compute();
            cartellino.AfterCompute();

            return cartellino;
        }

        public ITimesheet Create(Cant cantiere, DateTime month, TimesheetParams parameters)
        {
            ITimesheet cartellino = null;

            DateTime monthRequested = Common.CommonService.GetFirstMonthDay(month);

            TimesheetConfig<Cant> config = CreateConfig(cantiere, monthRequested, parameters);

            switch (parameters.Cartellino_Grouped_Entity_Type)
            {
                case Common.CartellinoGroupedEntityEnum.Collaboratore:
                    cartellino = new CantTimesheetDividedByCol(config, registrationProvider);
                    break;
                case Common.CartellinoGroupedEntityEnum.None:
                    cartellino = new CantTimesheet(config, registrationProvider);
                    break;
            }

            cartellino.BeforeCompute();
            cartellino.Compute();
            cartellino.AfterCompute();

            return cartellino;
        }

        private IEnumerable<ITimesheet> Cycle<T>(IEnumerable<T> entities, Func<T, ITimesheet> action) where T : class
        {
            var cartellini = new List<ITimesheet>();

            foreach (T entity in entities)
            {
                var cartellino = action(entity);
                cartellini.Add(cartellino);
            }

            return cartellini;
        }

        private TimesheetConfig<Cant> CreateConfig(Cant entità, DateTime monthRequested, TimesheetParams @params)
        {
            DateTime minCalculationDate = CartellinoHelper.CalculateMinDate(monthRequested, @params);
            DateTime maxCalculationDate = CartellinoHelper.CalculateMaxDate(monthRequested, @params);
            DateTime minSummaryDate = CartellinoHelper.CalculateMinSummaryDate(monthRequested, @params);
            DateTime maxSummaryDate = CartellinoHelper.CalculateMaxSummaryDate(monthRequested, @params);

            return new TimesheetConfig<Cant>()
            {
                Entità = entità,
                MinDate = minCalculationDate,
                MaxDate = maxCalculationDate,
                MinSummaryDate = minSummaryDate,
                MaxSummaryDate = maxSummaryDate,
                MonthRequested = monthRequested,
                Params = @params
            };
        }

        private TimesheetConfig<Col> CreateConfig(Col entità, DateTime monthRequested, TimesheetParams @params)
        {
            DateTime minCalculationDate = CartellinoHelper.CalculateMinDate(monthRequested, @params);
            DateTime maxCalculationDate = CartellinoHelper.CalculateMaxDate(monthRequested, @params);
            DateTime minSummaryDate = CartellinoHelper.CalculateMinSummaryDate(monthRequested, @params);
            DateTime maxSummaryDate = CartellinoHelper.CalculateMaxSummaryDate(monthRequested, @params);

            return new TimesheetConfig<Col>()
            {
                Entità = entità,
                MinDate = minCalculationDate,
                MaxDate = maxCalculationDate,
                MinSummaryDate = minSummaryDate,
                MaxSummaryDate = maxSummaryDate,
                MonthRequested = monthRequested,
                Params = @params
            };
        }
    }
}
