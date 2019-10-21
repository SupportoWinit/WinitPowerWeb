using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Business.Synchronization.OperationStrategies.GeoBadge
{
    public class GeoBadgeColStrategies : IOperationStrategies<Col>
    {
        public JArray ExecuteAddStrategy(IEnumerable<Col> entities)
        {
            return JArray.FromObject(entities.Select(col => new {
                Nome = col.Nome_Col,
                Cognome = col.Cognome_Col
            }).ToArray());
        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Col> entities)
        {
            return JArray.FromObject(entities.Select(col => new {
                //IdLavoratore = col.IdLavoratore,
            }).ToArray());
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Col> entities)
        {
            throw new NotImplementedException(); return JArray.FromObject(entities.Select(col => new {
                //IdLavoratore = col.IdLavoratore,
            }).ToArray());
        }
    }
}
