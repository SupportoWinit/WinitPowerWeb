using System.Collections.Generic;
using Domain;

namespace Business.Repository.Custom
{
    public interface ICantRepository : IRepository<Cant>
  {
        void UpdateGeoLocation(Cant cant);

        Dictionary<string, string> ImportFromCSV(string[] inputCants, int Fil_Id);

      void AddPendingElabForActivity(Cant entity);

      /// <summary>
      /// Inizializza un nuovo cantiere GPS a partire dalla latitudine e longitudine passata come parametro.
      /// </summary>
      /// <param name="cantLatitude">La latitudine del cantiere da generare.</param>
      /// <param name="cantLongitude">La longitudine del cantiere da generare.</param>
      /// <returns>Il nuovo cantiere gps con tutti i dati ricavabili</returns>
      Cant InitNewGpsCant(double cantLatitude, double cantLongitude);
  }
}
