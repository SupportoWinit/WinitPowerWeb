using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Domain;
using System.Data.Entity;
using Business.Repository;

namespace Business.Synchronization.OperationStrategies.ClockApp
{
    public class ClockAppColOperationStrategies : IOperationStrategies<Col>
    {
        public JArray ExecuteAddStrategy(IEnumerable<Col> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Added.ToString());

            JArray toShipEntities = new JArray();

            foreach (var col in entities)
            {
                JObject entity = new JObject();

                entity.Add("ColCode", col.Codice_Collaboratore);
                entity.Add("ColDes", col.CognomeNome_Col);
                entity.Add("ColMatr", col.Pru_Col.Any() ? col.Pru_Col.OrderByDescending(c => c.Abilitazione_Data_Inizio_Pru_Col).First().Codice_Pru : "NA");
                entity.Add("Disabilitato", col.DisAbilitazione_Col);
                entity.Add("PowerWebColId", col.Col_Id);

                toShipEntities.Add(entity);

            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;

            

        }

        public JArray ExecuteDeletionStrategy(IEnumerable<Col> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Deleted.ToString());
            obj.Add("Ids", JArray.FromObject(entities.Select(c => c.Col_Id).ToList()));

            result.Add(obj);

            return result;
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Col> entities)
        {
            JArray result = new JArray();

            JObject obj = new JObject();

            obj.Add("CustomerCode", RepoManager.ParamRepo.ParametersRow.Codice_Cliente);
            obj.Add("Op", EntityState.Modified.ToString());

            JArray toShipEntities = new JArray();

            foreach (var col in entities)
            {
                JObject entity = new JObject();

                entity.Add("ColCode", col.Codice_Collaboratore);
                entity.Add("ColDes", col.CognomeNome_Col);
                entity.Add("ColMatr", col.Pru_Col.Any() ? col.Pru_Col.OrderByDescending(c => c.Abilitazione_Data_Inizio_Pru_Col).First().Codice_Pru : "NA");
                entity.Add("Disabilitato", col.DisAbilitazione_Col);
                entity.Add("PowerWebColId", col.Col_Id);

                toShipEntities.Add(entity);

            }

            obj.Add("Entities", toShipEntities);

            result.Add(obj);

            return result;


        }
        public JArray ExecuteAddStrategy(IEnumerable<Cant> entities, IDictionary<string, string> connectionString)
        {
            throw new System.NotImplementedException();
        }

        public JArray ExecuteAddStrategy(IEnumerable<Col> entities, IDictionary<string, string> connectionString)
        {
            throw new NotImplementedException();
        }

        public JArray ExecuteModifyStrategy(IEnumerable<Col> entities, IDictionary<string, string> connectionString)
        {
            throw new NotImplementedException();
        }

        public JArray ExecuteDeleteStrategy(IEnumerable<Col> entities, IDictionary<string, string> connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
