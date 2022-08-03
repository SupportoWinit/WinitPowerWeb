using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Domain;
using Business.Repository;
using System.Data.Entity;

namespace Business.Synchronization.OperationStrategies.Flutter
{
    public class FlutterCantOperationStrategies : IOperationStrategies<Cant>
    {
        private IDictionary<string, string> connectionConfig { get; set; }
        public JArray ExecuteAddStrategy(IEnumerable<Cant> entities, IDictionary<string, string> connectionConfig)
        {
            int index = RepoManager.ParamRepo.ParametersRow.Indice_Timbrature_FlutterApp;
            JArray result = new JArray();
            int IdCliente = Int32.Parse(connectionConfig["IdCliente"]);
            JObject request = JObject.FromObject(new
            {
                IdCliente = connectionConfig["IdCliente"]
            });

            JObject obj = new JObject();

            obj.Add("idCliente", IdCliente);

            JArray toShipEntities = new JArray();

            foreach (var cant in entities)
            {
                JObject entity = new JObject();
                entity.Add("idCliente", IdCliente);
                entity.Add("descrizione", cant.Descrizione_Can);
                entity.Add("UnitaFissa", cant.Cant_Id);
                toShipEntities.Add(entity);
            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Cant> entities, IDictionary<string, string> connectionConfig)
        {
            JArray result = new JArray();

            int IdCliente = Int32.Parse(connectionConfig["IdCliente"]);

            JObject obj = new JObject();

            obj.Add("idCliente", IdCliente);

            JArray toShipEntities = new JArray();

            foreach (var cant in entities)
            {
                JObject entity = new JObject();
                entity.Add("idCliente", IdCliente);
                entity.Add("descrizione", cant.Descrizione_Can);
                entity.Add("UnitaFissa", cant.Cant_Id);
                toShipEntities.Add(entity);
            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;
        }

        public JArray ExecuteDeleteStrategy(IEnumerable<Cant> entities, IDictionary<string, string> connectionConfig)
        {
            JArray result = new JArray();

            int IdCliente = Int32.Parse(connectionConfig["IdCliente"]);

            JObject obj = new JObject();

            JArray toShipEntities = new JArray();

            foreach (var cant in entities)
            {
                JObject entity = new JObject();
                entity.Add("idCliente", IdCliente);
                entity.Add("descrizione", cant.Descrizione_Can);
                entity.Add("UnitaFissa", cant.Cant_Id);
                toShipEntities.Add(entity);
            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;
        }

        public JArray ExecuteAddStrategy(IEnumerable<Cant> entities)
        {
            throw new NotImplementedException();
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Cant> entities)
        {
            throw new NotImplementedException();
        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Cant> entities)
        {
            throw new NotImplementedException();
        }
    }
}
