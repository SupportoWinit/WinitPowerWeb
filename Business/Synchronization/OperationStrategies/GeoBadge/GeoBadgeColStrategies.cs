using Domain;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Business.Synchronization.OperationStrategies.GeoBadge
{
    public class GeoBadgeColStrategies : IOperationStrategies<Col>
    {
        public JArray ExecuteAddStrategy(IEnumerable<Col> entities)
        {
            return JArray.FromObject(entities.Select(col => new
            {
                Nome = col.Nome_Col,
                Cognome = col.Cognome_Col
            }).ToArray());
        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Col> entities)
        {
            return JArray.FromObject(entities.Select(col => new
            {
                //IdLavoratore = col.IdLavoratore,
            }).ToArray());
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Col> entities)
        {
            return JArray.FromObject(entities.Select(col => new
            {
                //IdLavoratore = col.IdLavoratore,
            }).ToArray());
        }
    }
}
