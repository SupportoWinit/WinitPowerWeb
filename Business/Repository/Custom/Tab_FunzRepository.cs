using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Business.Properties;
using Common;

namespace Business.Repository.Custom
{
  public class Tab_FunzRepository : GenericRepository<Tab_Funz>, ITab_FunzRepository
  {
     public Tab_FunzRepository(PowerWebEntities context)
            : base(context)
        {
        }

    private static void ResetSession()
    {
    }

    public override Dictionary<string, string> Check(Tab_Funz entity, bool isNew = false, bool isResetSession = true)
    {
      Dictionary<string, string> result = new Dictionary<string, string>();
    //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
        if (isResetSession)
            ResetSession();

        try
        {  
          //  
          //NON ESEGUO il Controllo sulla DataOraUltimaModifica perchè questa tabella NON ha questa informazione
          //      
          //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
          //
          if (CommonService.Nz(entity.Nome_Tab_Funz, "") == "")
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab_Funz),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_TAB_FUNZ));
          else
          {
            if (isNew)
            {
              if (RepoManager.Tab_FunzRepo.SingleOrDefault(u => u.Nome_Tab_Funz == entity.Nome_Tab_Funz) != null)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab_Funz),
               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
            else
            {
              if (RepoManager.Tab_FunzRepo.SingleOrDefault(u => u.Nome_Tab_Funz == entity.Nome_Tab_Funz && u.Tab_Funz_Id != entity.Tab_Funz_Id) != null)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab_Funz),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
          }
          // Verifico che l'Utente che sta modificando la Tabella abbia livello >=12
          if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Funz_Id),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO_TABELLA)); 
          //
          //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
          //  
          if (CommonService.Nz(entity.Link_Tab_Funz, "") == "")
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Link_Tab_Funz),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_LINK_TAB_FUNZ));
          if (CommonService.Nz(entity.Nome_Tab_Funz, "") == "")
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Tab_Funz),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_TAB_FUNZ));
          //
          //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
          //
          //
          //4) verifico, per una serie di campi, che il valore del campo sia corretto
          //
          //
          //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
          //            
          //
          //6) Scrittura del Record di LOG
          //
          WriteCheckLog(entity, result, Log);
        }
        catch (Exception ex)
        {
            var CodErr = "Tab_Funz_Id: " + entity.Tab_Funz_Id;
            throw ex;
        }
        return result;
    }
  }
}
