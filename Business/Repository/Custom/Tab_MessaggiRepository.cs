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
    class Tab_MessaggiRepository : GenericRepository<Tab_Messaggi>, ITab_MessaggiRepository
    {
        public Tab_MessaggiRepository(PowerWebEntities context):base(context)
        { }

        public void InsertMessages(List<KeyValuePair<String, String>> messageList, ApplicationMessageEnum application, FunctionMessageEnum function, int? elaborateUserId = null, DateTime? elaborateDateTime = null)
        {
            var messages = new List<Tab_Messaggi>();
            var date = DateTime.UtcNow;
            foreach (var message in messageList)
            {
                var currMessage = RepoManager.Tab_MessaggiRepo.Init();

                currMessage.Data_Tab_Messaggi = date;
                currMessage.Applicazione_Tab_Messaggi_Id = (int)application;
                currMessage.Testo_Tab_Messaggi = message.Value;
                currMessage.Funzione_Tab_Messaggi_Id = (int)function;
                currMessage.Utente_Id = elaborateUserId;
                currMessage.Data_Ora_Elab_Tab_Messaggi = elaborateDateTime;
                messages.Add(currMessage);
            }
            try
            {
                RepoManager.Tab_MessaggiRepo.Context.BulkInsert(messages);
            }
            catch(Exception ex)
            {
                
            }
            
        }
        

        
    }
}
