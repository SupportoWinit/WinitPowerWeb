using System;
using System.Collections.Generic;
using Domain;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Business.Synchronization.OperationStrategies.GeoBadge
{
    public class GeoBadgeCantStrategies : IOperationStrategies<Cant>
    {
        public JArray ExecuteAddStrategy(IEnumerable<Cant> entities)
        {
            return JArray.FromObject(entities.Select(cant => new
            {
                Codice = cant.Codice_Cantiere,
                Descrizione = cant.Descrizione_Can
                //IdTerminale = cant.IdTerminale ?? 0
            }).ToArray());
        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Cant> entities)
        {
            return JArray.FromObject(entities.Select(cant => new
            {
                Codice = cant.Codice_Cantiere,
                Descrizione = cant.Descrizione_Can,
                //IdTerminale = cant.IdTerminale 
            }).ToArray());
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Cant> entities)
        {
            return JArray.FromObject(entities.Select(cant => new
            {
                //IdTerminale = cant.IdTerminale 
            }).ToArray());
        }
    }
}
