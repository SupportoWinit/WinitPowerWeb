using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Business.MDBSchema;
using System.Data;
using Common;

namespace Business.Repository.Custom
{
  public interface ITab_MessaggiRepository : IRepository<Tab_Messaggi>
  {
      void InsertMessages(List<KeyValuePair<String, String>> messageList, ApplicationMessageEnum application, FunctionMessageEnum function, int? elaborateUserId = null, DateTime? elaborateDateTime = null);
  }
}
