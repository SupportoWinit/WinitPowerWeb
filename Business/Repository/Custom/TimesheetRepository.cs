using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Linq.Expressions;
using System.Data;
using System.Data.OleDb;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class TimesheetRepository : GenericRepository<Timesheet>, ITimesheetRepository
    {
        public TimesheetRepository(PowerWebEntities context)
            : base(context)
        {
        }
        public override Dictionary<string, string> Check(Timesheet entity, bool isNew = false, bool isResetSession = true)
        {
            return base.Check(entity, isNew, isResetSession);
        }

        public override Timesheet Init()
        {
            return base.Init();
        }

        /// <summary>
        /// Somma due oggetti di tipo Timesheet in nu nuovo timesheet.
        /// </summary>
        /// <param name="timesheet1">Il primo timesheet.</param>
        /// <param name="timesheet2">Il secondo timesheet.</param>
        /// <param name="justification">La motivazione del cartellino da creare.</param>
        /// <param name="colId">L'ID del collaboratore a cui attribuire il nuovo timesheet.</param>
        /// <param name="month">Il mese a cui si riferisce il nuovo timesheet.</param>
        /// <returns></returns>
        public Timesheet mergeTimesheets (Timesheet timesheet1, Timesheet timesheet2, string justification, int colId, DateTime month)
        {
            Timesheet mergedTimesheet = new Timesheet();
            mergedTimesheet.Justification = justification;
            mergedTimesheet.ColId = colId;
            mergedTimesheet.Month = month;
            string property = "";

            for (int i = CommonService.GetFirstMonthDay(month).Day, lastDay = CommonService.GetLastMonthDay(month).Day; i <= lastDay; i++)
            {
                property = "Day" + i.ToString("00");
                mergedTimesheet[property] = ((TimeSpan)timesheet1[property]).Add((TimeSpan)timesheet2[property]);
            }
            
            return mergedTimesheet;
        }

    }
}