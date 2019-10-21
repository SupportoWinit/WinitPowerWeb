
using System;
using Business.Repository;
using Domain;
using System.Linq;
using System.Collections.Generic;
using Common;
using Business.BusinessServices.TimesheetService.TimesheetUpdate.Interface;
using Business.BusinessClasses.CartellinoServiceDTOs;

namespace Business.BusinessServices.TimesheetService.CartellinoUpdate.Implementations
{


    public class CantCartellinoCellUpdate : ITimesheetUpdate
    {
        private Tab_Decod _currentMotivation;
        public void Update(TimesheetCellUpdate update)
        {
            _currentMotivation = Helpers.CartellinoHelper.MotivazioniByKey[update.Justification];

            if (update.Time == TimeSpan.Zero) //indica cancellazione
                DeleteJustificationOfTheDay(update);
            else
            {
                DeleteJustificationOfTheDay(update); //cancello-pareggio reg esistenti
                GenerateNewJustification(update); //genero la nuova reg
            }
        }

        public void UpdateRettifica(TimesheetCellUpdate update)
        {
            RepoManager.Reg_VRepo.DbSet.Where(x =>
                                              x.Cant_Id == update.BaseEntity_Id &&
                                              x.Col_Id == update.GroupedEntity_Id &&
                                              x.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual &&
                                              x.Data_Reg == update.Date).DeleteFromQuery();

            if (update.Time != TimeSpan.Zero)
            {

                GenerateNewRettifica(update); //genero la nuova reg
            }
        }


        /// <summary>
        /// Elimina le reg per un determinato giorno,cantiere,collaboratore e codice motivazione.
        /// Inoltre pareggia le registrazioni inserite dalla manutenzione timbrature
        /// che non sono di sola durata
        /// </summary>
        /// <param name="justificationKey">la chiave tab_Decod per quella registrazione.</param>
        /// <param name="date">The date.</param>
        void DeleteJustificationOfTheDay(TimesheetCellUpdate update)
        {
            //Ricerco la motivazione richiesta

            //Recupero gli id per la cancellazione delle reg di sola durata
            var deletionQuery = RepoManager.Reg_VRepo.DbSet.Where(x =>
                                                                  x.Col_Id == update.GroupedEntity_Id
                                                                  && x.Cant_Id == update.BaseEntity_Id
                                                                  && x.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration
                                                                  && x.Data_Reg == update.Date
                                                                  && x.Motivazione_Reg_Id == _currentMotivation.Tab_Decod_Id);



            IEnumerable<int> onlyDurRegs = deletionQuery.Select(x => x.RegE).ToList();


            //Se abbiamo delle reg di sola durata le cancelliamo
            if (onlyDurRegs.Any())
            {
                foreach (var regVEntranceId in onlyDurRegs)
                {
                    Reg toDelete = new Reg() { Reg_Id = regVEntranceId };
                    RepoManager.RegRepo.Attach(toDelete);
                    RepoManager.RegRepo.Context.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                }

                RepoManager.RegRepo.SaveChanges();
            }

            //Calcolo la durata di tali registrazioni per tale giorno, cantiere, collaboratore
            int regEU_Duration = RepoManager.Reg_VRepo.DbSet.AsNoTracking()
                                                            .Where(x =>
                                                                   x.Col_Id == update.GroupedEntity_Id
                                                                   && x.Cant_Id == update.BaseEntity_Id
                                                                   && x.Registrazione_Tipo_Reg == 0
                                                                   && x.Data_Ora_Fig_E != null
                                                                   && x.Data_Ora_Fig_U != null
                                                                   && x.Data_Reg == update.Date)
                                                             .Select(x => x.Durata_Fis)
                                                             .Sum() ?? 0;

            TimeSpan totalDayDuration = TimeSpan.FromMinutes((double)regEU_Duration);

            if (totalDayDuration == TimeSpan.Zero)
                return;

            //Se abbiamo delle registrazioni  E - U non le cancelliamo ma facciamo in modo di annullarle tramite una nuova reg - negativa
            if (totalDayDuration - update.Time != TimeSpan.Zero)
                GenerateEqualizeReg(update, totalDayDuration);
        }

        void GenerateEqualizeReg(TimesheetCellUpdate update, TimeSpan totalDayDuration)
        {

            var cte = (totalDayDuration - update.Time < TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;
            var correctionRegDuration = (totalDayDuration - update.Time).Duration();
            var newCorrectionReg = RepoManager.RegRepo.GenerateCorrectionReg(update.GroupedEntity_Id, update.Date, cte, correctionRegDuration, update.BaseEntity_Id);

            RepoManager.RegRepo.Add(newCorrectionReg, true);

        }

        void GenerateNewJustification(TimesheetCellUpdate update)
        {
            var cte = (update.Time > TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;
            var newDurationReg = RepoManager.RegRepo.GenerateDurationReg(update.GroupedEntity_Id, update.Date, cte, (update.Time).Duration(), update.BaseEntity_Id, _currentMotivation.Tab_Decod_Id);

            RepoManager.RegRepo.Add(newDurationReg, true);

        }

        void GenerateNewRettifica(TimesheetCellUpdate update)
        {
            var cte = (update.Time > TimeSpan.Zero) ? CorrectionTypeEnum.CorrectionPlus : CorrectionTypeEnum.CorrectionMinus;
            var newDurationReg = RepoManager.RegRepo.GenerateManualRett(update.GroupedEntity_Id, update.Date, cte, (update.Time).Duration(), update.BaseEntity_Id);
            try{
                RepoManager.RegRepo.Add(newDurationReg, true);
            }
            catch(Exception ex)
            {

            }
            
        }
    }
}
