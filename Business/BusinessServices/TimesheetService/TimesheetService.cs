using Business.BusinessClasses;
using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessServices.TimesheetService.CartellinoUpdate.Implementations;
using Business.BusinessServices.TimesheetService.Enums;
using Business.BusinessServices.TimesheetService.Factory;
using Business.BusinessServices.TimesheetService.Helpers;
using Business.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using Business.BusinessServices.CartellinoService.Interfaces.Timesheet;
using Business.BusinessServices.TimesheetService.TimesheetUpdate.Interface;

namespace Business.BusinessServices.TimesheetService
{
    public class TimesheetService : ITimesheetService
    {
        private CartellinoFactory _cartellinoFactory;

        private TimesheetParams _tempParams;

        public TimesheetParams TempParams
        {
            get
            {
                try
                {
                    return _tempParams;
                }
                finally
                {
                    _tempParams = null;
                }
            }
            set { _tempParams = value; }
        }

        private IDictionary<TimesheetBaseType, ITimesheetUpdate> cartelliniUpdatesManager;

        public TimesheetService()
        {
            _cartellinoFactory = new CartellinoFactory();


            cartelliniUpdatesManager = new Dictionary<TimesheetBaseType, ITimesheetUpdate>();

            cartelliniUpdatesManager.Add(TimesheetBaseType.Col, new ColTimesheetCellUpdate());
            cartelliniUpdatesManager.Add(TimesheetBaseType.Cant, new CantCartellinoCellUpdate());
        }

        /// <summary>
        /// Salva le modifiche effettuate dal cartellino presenze
        /// </summary>
        /// <param name="update">Oggetto contente i dati dell'update.</param>
        /// <returns></returns>
        public ClientResponse SaveColCartellinoCellUpdate(TimesheetCellUpdate update)
        {
            ClientResponse response;
            if (!SaveUpdatesHelper.ValidateColCartellinoUpdateDto(update, out response))
                return response; //settare errori

            if (update.IsRettificaUpdate)
                cartelliniUpdatesManager[update.UpdateType].UpdateRettifica(update);
            else
                cartelliniUpdatesManager[update.UpdateType].Update(update);

            return new ClientResponse
            {
                StatusCode = System.Net.HttpStatusCode.OK
            };

        }

        /// <summary>
        /// Elabora il cartellino in base agli id di dominio richiesti e all'entità
        /// di base (fin'ora cantiere e collaboratore) per un determinato mese
        /// </summary>
        /// <param name="ids">Gli id di dominio.</param>
        /// <param name="date">La data del mese richiesto.</param>
        /// <param name="entityType">Il tipo di entità di riferimento.</param>
        /// <param name="cartellinoOptions">I parametri correnti del cartellino.</param>
        /// <returns></returns>
        public ITimesheet ElaborateCartellino(int id, DateTime date, TimesheetParams cartellinoOptions)
        {
            return _cartellinoFactory.Create(id, date, cartellinoOptions);
        }

        public ITimesheet ElaborateCartellino(int id, DateTime date)
        {
            return _cartellinoFactory.Create(id, date, this.GetParams());
        }

        public IEnumerable<ITimesheet> ElaborateCartellino(IEnumerable<int> ids, DateTime date, TimesheetParams cartellinoOptions)
        {
            return _cartellinoFactory.Create(ids, date, cartellinoOptions);
        }

        /// <summary>
        /// Elabora il cartellino con divisione notturna/straordinari
        /// </summary>
        /// <param name="ids">Gli id di dominio.</param>
        /// <param name="date">La data del mese richiesto.</param>
        /// <param name="cartellinoOptions">I parametri correnti del cartellino.</param>
        /// <returns></returns>
        public CartellinoStrDTO ElaborateStrCartellino(IEnumerable<int> ColIds, DateTime date, TimesheetParams options)
        {
            var collaboratori = RepoManager.ColRepo.Find(col => ColIds.Contains(col.Col_Id)).ToList();

            ICollection<CartellinoStrDTO> cartelliniElaborati = new List<CartellinoStrDTO>();

            collaboratori.ForEach(col =>
            {

                var minDate = CartellinoHelper.CalculateMinDate(date, options);
                var maxDate = CartellinoHelper.CalculateMaxDate(date, options);
                var regs = CartellinoStrHelper.GetRegVForStrTimesheet(col, minDate, maxDate);

                var cartellino = CartellinoStrHelper.GenerateStrCartellino(col, regs, minDate, maxDate.AddDays(-1));

                cartelliniElaborati.Add(cartellino);
            });

            return cartelliniElaborati.First();
        }

        #region Parametri


        /// <summary>
        /// Ritorna i parametri correnti.
        /// </summary>
        /// <returns></returns>
        public TimesheetParams GetParams()
        {
            var @params = RepoManager.ParamRepo.DbSet.First();

            return new TimesheetParams()
            {
                Abilita_Divisione_Cantiere = @params.Cartellino_Abilita_Divisione_Cantiere,
                Abilita_Modifica = @params.Cartellino_Abilita_Modifica,
                Abilita_MonteMinuti = @params.Abilita_Monte_Minuti,
                DataBlocco = @params.Data_Blocco_Reg,
                Dividi_Altra_Anagrafica = @params.Cartellino_Dividi_Altra_Anagrafica,
                Divisione_Piano_Notturno_Diurno = @params.Cartellino_Divisione_Piano_Notturno_Diurno,
                Totale_Prima_Colonna = @params.Cartellino_Totale_Prima_Colonna,
                Usa_Cartellino_Modificabile = @params.Cartellino_Usa_Cartellino_Modificabile,
                Usa_Rettifiche_Auto = @params.Cartellino_Usa_Rettifiche_Auto,
                Usa_Rettifiche_Cartellino = @params.Cartellino_Usa_Rettifiche_Cartellino,
                Usa_Rettifiche_Manuali = @params.Cartellino_Usa_Rettifiche_Manuali,
                Visualizza_Delta = @params.Cartellino_Visualizza_Delta,
                Visualizza_Motivazioni = @params.Cartellino_Visualizza_Motivazioni,
                Visualizza_Ore = @params.Cartellino_Visualizza_Ore,
                Visualizza_Piano = @params.Cartellino_Visualizza_Piano,
                Visualizza_Totale = @params.Cartellino_Visualizza_Totale,
                Visualizza_Totali_Settimanali = Convert.ToBoolean(@params.Cartellino_Visualizza_Totali_Settimanali),
                Visualizza_Viaggi = @params.Cartellino_Visualizza_Viaggi
            };
        }


        /// <summary>
        /// Salva i parametri a database.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public void SetParams(TimesheetParams parameters)
        {
            var dbParameters = RepoManager.ParamRepo.DbSet.First();

            dbParameters.Cartellino_Divisione_Piano_Notturno_Diurno = parameters.Divisione_Piano_Notturno_Diurno;
            dbParameters.Cartellino_Visualizza_Piano = parameters.Visualizza_Piano;
            dbParameters.Cartellino_Visualizza_Ore = parameters.Visualizza_Ore;
            dbParameters.Cartellino_Visualizza_Motivazioni = parameters.Visualizza_Motivazioni;
            dbParameters.Cartellino_Visualizza_Delta = parameters.Visualizza_Delta;
            dbParameters.Cartellino_Visualizza_Viaggi = parameters.Visualizza_Viaggi;
            dbParameters.Cartellino_Visualizza_Totale = parameters.Visualizza_Totale;
            dbParameters.Cartellino_Totale_Prima_Colonna = parameters.Totale_Prima_Colonna;
            dbParameters.Cartellino_Dividi_Altra_Anagrafica = parameters.Dividi_Altra_Anagrafica;
            dbParameters.Cartellino_Usa_Rettifiche_Cartellino = parameters.Usa_Rettifiche_Cartellino;
            dbParameters.Cartellino_Visualizza_Totali_Settimanali = Convert.ToInt32(parameters.Visualizza_Totali_Settimanali);

            RepoManager.ParamRepo.SaveChanges();


        }

      

        #endregion


    }
}
