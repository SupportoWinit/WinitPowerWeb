using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Domain;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
  public interface ITab_FestiviRepository : IRepository<Tab_Festivi>
  {
      bool IsHolidayOrNotWorkDays(DateTime date);

      DateTime NextValidDay(DateTime date);
  }  
}