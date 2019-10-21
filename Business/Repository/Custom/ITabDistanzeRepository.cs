using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Data;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
  public interface ITabDistanzeRepository : IRepository<Tab_Dist>
  {
    Tab_Dist Init();
  }
}
