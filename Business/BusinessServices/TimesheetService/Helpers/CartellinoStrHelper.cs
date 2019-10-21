using Business.BusinessClasses.CartellinoServiceDTOs;
using Business.BusinessExtensions;
using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Helpers
{
    public static class CartellinoStrHelper
    {
        
        public static IEnumerable<Reg_V> GetRegVForStrTimesheet(Col col, DateTime minDate, DateTime maxDate)
        {

            // si ricalcola il periodo di ricerca delle reg_v a partire dalle date di disponibilità del collaboratore
            Tuple<DateTime, DateTime> validSearchDates = RepoManager.Tab_OrariRepo.FilterPeriodWithColDispDates(minDate, maxDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col);
            DateTime newFirstMonthDate = validSearchDates.Item1;

            // si aggiunge un giorno nella verifica del fine mese in quanto alla mezzanotte di fine mese mancano ancora 24 ore di timbrature:
            // in pratica se cerco tutte le timbrature con data minore di 30/06 00:00 mi perdo tutte le timbrature dal 30/06 00:00 al 30/06 23:29
            DateTime newLastMonthDate = validSearchDates.Item2;

            // ritorno delle registrazioni calcolate con i parametri spassati come parametro che siano associate, non attività
            return RepoManager.Reg_VRepo.DbSet
                                        .AsNoTracking()
                                        .Where(regv =>
                                               regv.Col_Id == col.Col_Id
                                               && (regv.Data_Reg >= newFirstMonthDate && regv.Data_Reg < newLastMonthDate)
                                               && regv.Registrazione_Stato_Reg != (int)RegStateEnum.None
                                               && regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att)
                                        .OrderBy(regv => regv.Data_Reg)
                                        .ToList();

        }

        public static CartellinoStrDTO GenerateStrCartellino(Col col, IEnumerable<Reg_V> regs, DateTime minDate, DateTime maxDate)
        {
            var parameters = RepoManager.ParamRepo.ParametersRow;

            var completeTimesheet = new CartellinoStrDTO();

            var dayPlan = GenerateColPlan(col, minDate, maxDate, "Diurno");
            var nightPlan = GenerateColPlan(col, minDate, maxDate, "Notturno");

            var dayRegs = GetDayRegs(regs, col);
            var nightRegs = GetNightRegs(regs, col);

            var oreDiurne = GenerateTimesheet(col, dayRegs, PowerWebResources.LBL_ORE_DIURNE, minDate, maxDate);
            var oreNotturne = GenerateTimesheet(col, nightRegs, PowerWebResources.LBL_ORE_NOTTURNE, minDate, maxDate);

            var cartellini = GenerateOrdDiurneTimesheet(minDate, maxDate, oreDiurne, oreNotturne, dayPlan, nightPlan);



            completeTimesheet.AddTimesheet(oreDiurne.Label, oreDiurne);
            completeTimesheet.AddTimesheet(oreNotturne.Label, oreNotturne);

            foreach (var row in cartellini)
                completeTimesheet.AddTimesheet(row.Label,row);


            return completeTimesheet;

            }

        public static void GetPlanTimesheet(Col col, DateTime minDate, DateTime maxDate)
        {
            var planMinutes = RepoManager.Tab_OrariRepo.GetPlanTimes(col.Col_Id, minDate, maxDate, null, null);
            //var colPlan = GenerateNewPlanTimesheet(isDecimalHours, planMinutes.First().Value, isFromFreeTimesheet, freeTimesheetId, col.Col_Id, minDate, planMinutes.First().Key);


        }

        static IEnumerable<Reg_V> GetDayRegs(IEnumerable<Reg_V> regs, Col col)
        {
            TimeSpan? nocturnStartHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Inizio_Notturno ?? new TimeSpan(10,0,0);
            TimeSpan? nocturnStartHourModify = null;
            TimeSpan? nocturnEndHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Fine_Notturno ?? new TimeSpan(06, 0, 0);

            //if (colPlan != default(TimesheetModuleItem))
            //{
            //    nocturnStartHour = colPlan.NocturnsStartHour ?? TimeSpan.Zero;
            //    nocturnEndHour = colPlan.NocturnEndHour ?? TimeSpan.Zero;
            //}

            TimeSpan oraIn_Nott = nocturnStartHour.Value;
            TimeSpan oraOut_Nott = nocturnEndHour.Value > nocturnStartHour.Value ? nocturnEndHour.Value : nocturnEndHour.Value.Add(new TimeSpan(1, 0, 0, 0));


            var filteredRegs = regs
                .Where(regv =>
                {
                    if (!regv.Data_Ora_Fig_U.HasValue || !regv.Data_Ora_Fig_E.HasValue)
                    {
                        return true;
                    }

                    TimeSpan oraE = regv.Data_Ora_Fig_E.Value.TimeOfDay;
                    TimeSpan oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay;

                    if (regv.Data_Ora_Fig_U.Value.Date > regv.Data_Ora_Fig_E.Value.Date)
                    {
                        oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay.Add(new TimeSpan(1, 0, 0, 0));
                    }

                    else
                    {
                        if (oraE <= nocturnEndHour.Value && oraU <= nocturnEndHour.Value)
                        {
                            oraE = oraE.Add(new TimeSpan(1, 0, 0, 0));
                            oraU = oraU.Add(new TimeSpan(1, 0, 0, 0));
                        }
                    }

                    if (oraE < oraIn_Nott || oraU > oraOut_Nott)
                    {
                        return true;
                    }
                    return false;

                }).ToList();



            return filteredRegs;
        }

        static IEnumerable<Reg_V> GetNightRegs(IEnumerable<Reg_V> regs, Col col)
        {
            TimeSpan? nocturnStartHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Inizio_Notturno ?? new TimeSpan(10, 0, 0); ;
            TimeSpan? nocturnStartHourModify = null;
            TimeSpan? nocturnEndHour = RepoManager.ParamRepo.ParametersRow.Cartellino_Fine_Notturno ?? new TimeSpan(06, 0, 0); ;

            TimeSpan oraIn_Nott = nocturnStartHour.Value;
            TimeSpan oraOut_Nott = nocturnEndHour.Value > nocturnStartHour.Value ? nocturnEndHour.Value : nocturnEndHour.Value.Add(new TimeSpan(1, 0, 0, 0));


            var filteredRegs = regs.Where(regv => regv.Data_Ora_Fis_U.HasValue).
                    Where(regv =>
                    {

                        //Se per qualche motivo la registrazione non ha entrata, non è notturna
                        if (!regv.Data_Ora_Fig_E.HasValue)
                        {
                            return false;
                        }

                        /*Aggiunge 1 giorno a entrata e uscita se richiesto in modo da poterle confrontare agevolmente con inizio/fine notturno*/

                        TimeSpan oraE = regv.Data_Ora_Fig_E.Value.TimeOfDay;
                        TimeSpan oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay;

                        //Se entrata e uscita sono su due giorni diversi, l'uscita avrà un giorno in più
                        if (regv.Data_Ora_Fig_U.Value.Date > regv.Data_Ora_Fig_E.Value.Date)
                        {
                            oraU = regv.Data_Ora_Fig_U.Value.TimeOfDay.Add(new TimeSpan(1, 0, 0, 0));
                        }

                        else
                        {
                            //Se E e U sono sullo stesso giorno e vengono entrambe prima della fine del notturno, sono entrambe nel secondo giorno
                            if (oraE <= nocturnEndHour.Value && oraU <= nocturnEndHour.Value)
                            {
                                oraE = oraE.Add(new TimeSpan(1, 0, 0, 0));
                                oraU = oraU.Add(new TimeSpan(1, 0, 0, 0));
                            }
                        }

                        //C'è del notturno nei casi in cui non c'è diurno (se la registrazione è completamente a 'sinistra' o a 'destra' del notturno
                        if ((!((oraE < oraIn_Nott && oraU <= oraIn_Nott) || (oraE >= oraOut_Nott && oraU > oraOut_Nott))) ||
                                oraE < nocturnEndHour)
                        {
                            return true;
                        }

                        return false;


                    }).ToList();

            return filteredRegs;
        }

        static TimesheetModuleItem GenerateColPlan(Col col, DateTime minDate, DateTime maxDate, string tipoPiano)
        {
            bool isFreeTimesheet;
            int freeTimesheetId;

            var plans = RepoManager.Tab_OrariRepo.GetDevidedPlanMinutes(col.Col_Id, minDate, maxDate, col.Data_Disponibilita_Inizio_Col, col.Data_Disponibilita_Fine_Col, out isFreeTimesheet, out freeTimesheetId);

            return GenerateNewPlanTimesheet(false, plans[tipoPiano], false, 0, col.Col_Id, minDate, 0);

        }

        static CartellinoRow GenerateTimesheet(Col col, IEnumerable<Reg_V> regs, PowerWebResources resource, DateTime minDate, DateTime maxDate)
        {
            var dto = new CartellinoRow();
            dto.Label = BusinessService.GetLocalizedString(resource);

            var groupedRegs = regs.ToLookup(reg => reg.Data_Reg);

            foreach (var date in CommonService.EachDay(minDate, maxDate))
            {
                if (groupedRegs.Contains(date))
                {
                    dto.Days.Add(new CartellinoRowDay
                    {
                        Day = date,
                        Duration = TimeSpan.FromMinutes((double)groupedRegs[date].Sum(reg => reg.Durata_Fig))
                    });
                }
                else
                {
                    dto.Days.Add(new CartellinoRowDay
                    {
                        Day = date,
                        Duration = TimeSpan.Zero
                    });

                }


            }


            return dto;
        }

        static TimesheetModuleItem GenerateNewPlanTimesheet(bool isDecimalHours, Dictionary<DateTime, Tuple<double, TimeSpan?, TimeSpan?>> planMinutes,
            bool isFromFreeTimesheet, int freeTimesheetId, int colId, DateTime firstMonthDate, int cantId, PlanTypeEnum planType = PlanTypeEnum.Normal)
        {
            var planTimesheetItem = new TimesheetModuleItem(isDecimalHours);
            planTimesheetItem.StartDate = firstMonthDate;
            planTimesheetItem.PopulateHoursWithDate(planMinutes);
            planTimesheetItem.IsFromFreeTimeSheet = isFromFreeTimesheet;
            planTimesheetItem.FreeTimeSheetId = freeTimesheetId;
            planTimesheetItem.InsertColValues(colId);
            planTimesheetItem.InsertCantValues(cantId);
            planTimesheetItem.Order = 0;

            switch (planType)
            {
                case PlanTypeEnum.Normal:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);
                    break;
                case PlanTypeEnum.Day:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_DAY);
                    break;
                case PlanTypeEnum.Night:
                    planTimesheetItem.Justification = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN_NIGHT);
                    break;
                default:
                    break;
            }
            return planTimesheetItem;
        }

        static IEnumerable<CartellinoRow> GenerateOrdDiurneTimesheet(DateTime minDate, DateTime maxDate, CartellinoRow dayWorked, CartellinoRow nightWorked, TimesheetModuleItem dayPlan, TimesheetModuleItem nightPlan)
        {
            List<CartellinoRow> timesheets = new List<CartellinoRow>();
            
            var tsOrdDiurne = new CartellinoRow(); tsOrdDiurne.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU").Decodifica_Tab;
            var tsStrDiurne = new CartellinoRow(); tsStrDiurne.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU").Decodifica_Tab;
            var tsOrdNot = new CartellinoRow(); tsOrdNot.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT").Decodifica_Tab;
            var tsStrNot = new CartellinoRow(); tsStrNot.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT").Decodifica_Tab;
            var tsFestDiu = new CartellinoRow(); tsFestDiu.Label =  RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_DIU_FEST").Decodifica_Tab;
            var tsStrFest = new CartellinoRow(); tsStrFest.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_DIU_FEST").Decodifica_Tab;
            var tsOrdNotFest = new CartellinoRow(); tsOrdNotFest.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_ORD_NOT_FEST").Decodifica_Tab;
            var tsStrNotFest = new CartellinoRow(); tsStrNotFest.Label = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == "LBL_STR_NOT_FEST").Decodifica_Tab;
            
            int i = 1;
            foreach (var date in CommonService.EachDay(minDate, maxDate))
            {
                double ordDiu = 0;
                double strDiu = 0;
                double ordNot = 0;
                double strNot = 0;
                double pianoNotturne = CommonService.FromHoursToMinutes((double)nightPlan["Day" + i.ToString("00")]);
                double pianoDiurne = CommonService.FromHoursToMinutes((double)dayPlan["Day" + i.ToString("00")]);
                double effettiveDiurne = dayWorked.GetDoubleDuration(date);
                double effettiveNotturne = nightWorked.GetDoubleDuration(date);


                /* ASSEGNAZIONE AUTOMATICA PIANO DIURNO/NOTTURNO */
                // Se il flag sul orarioTipo è attivato e l'orario è solo durata
                // decide se il piano è diurno o notturno a seconda delle ore lavorative diurne e notturne effettuate
                //if (assignNotDiuAutomatically)
                //{
                //    if (pianoNotturne == 0 && effettiveNotturne > effettiveDiurne)
                //    {
                //        pianoNotturne = pianoDiurne;
                //        pianoDiurne = 0;
                //    }
                //}

                /* AGGIUSTAMENTI PIANI */
                //Se ho fatto meno diurne XOR meno notturne del previsto, bisogna aggiustare i piani
                //per evitare di segnare straordinarie che non sono state fatte

                if (effettiveDiurne <= pianoDiurne && effettiveNotturne > pianoNotturne)
                {
                    pianoNotturne = CommonService.SumDoubleHours(pianoNotturne, CommonService.SubtractDoubleHours(pianoDiurne, effettiveDiurne));
                }

                else if (effettiveNotturne <= pianoNotturne && effettiveDiurne > pianoDiurne)
                {
                    pianoDiurne = CommonService.SumDoubleHours(pianoDiurne, CommonService.SubtractDoubleHours(pianoNotturne, effettiveNotturne));
                }

                /* DIVISIONE ORDINARIE/STRAORDINARIE */
                //Se ci sono più ore lavorate che piano, assegna lo straordinario
                //Altrimenti non c'è straordinario

                if (effettiveDiurne > CommonService.FromHoursToMinutes(pianoDiurne))
                {
                    strDiu = CommonService.SubtractDoubleHours(effettiveDiurne,pianoDiurne);
                    ordDiu = pianoDiurne;
                }
                else
                {
                    strDiu = 0;
                    ordDiu = effettiveDiurne;
                }

                if (effettiveNotturne > pianoNotturne)
                {
                    strNot = CommonService.SubtractDoubleHours(effettiveNotturne, pianoNotturne);
                    ordNot = pianoNotturne;
                }
                else
                {
                    strNot = 0;
                    ordNot = effettiveNotturne;
                }


                /* ASSEGNAZIONE FESTIVO */
                //Se la giornata in elaborazione è festiva, le ore appena suddivise vanno nei cartellini festivi
                //Altrimenti vanno nei cartellini non festivi

                if (RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(date))
                {
                    tsOrdDiurne.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsStrDiurne.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsOrdNot.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsStrNot.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsFestDiu.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(ordDiu) });
                    tsStrFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(strDiu) });
                    tsOrdNotFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(ordNot) });
                    tsStrNotFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(strNot) });
                }
                else
                {
                    tsOrdDiurne.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(ordDiu) });
                    tsStrDiurne.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(strDiu) });
                    tsOrdNot.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(ordNot) });
                    tsStrNot.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.FromMinutes(strNot) });
                    tsFestDiu.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsStrFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsOrdNotFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                    tsStrNotFest.Days.Add(new CartellinoRowDay { Day = date, Duration = TimeSpan.Zero });
                }

                i++;
            }

            timesheets.Add(tsOrdDiurne);
            timesheets.Add(tsStrDiurne);
            timesheets.Add(tsOrdNot);
            timesheets.Add(tsStrNot);
            timesheets.Add(tsFestDiu);
            timesheets.Add(tsStrFest);
            timesheets.Add(tsOrdNotFest);
            timesheets.Add(tsStrNotFest);

            return timesheets;
        }
    }
}
