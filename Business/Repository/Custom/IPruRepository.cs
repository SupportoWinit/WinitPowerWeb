using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
  //Dichiarazione delle Funzioni da Richiamare
  public interface IPruRepository : IRepository<Pru>
  {
      Dictionary<string, string> ImportFromTXT(String[] inputFile);
  }
}
