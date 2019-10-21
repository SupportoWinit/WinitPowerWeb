using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
  public class Tab_ProvRepository : GenericRepository<Tab_Prov>, ITab_ProvRepository
  {
    public Tab_ProvRepository(PowerWebEntities context)
      : base(context)
    {
    }

    private static void ResetSession()
    {
    }

    public override Dictionary<string, string> Check(Tab_Prov entity, bool isNew = false, bool isResetSession = true)    
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
          // Se sto INSERENDO UN NUOVO RECORD verifico che NON esista già un Record con la stessa Sigla Provincia
          if (String.IsNullOrEmpty(entity.Sigla_Prov))
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Prov),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_SIGLA_PROV));
          else
            {
              if (isNew)
              {
                if (RepoManager.Tab_ProvRepo.SingleOrDefault(u => u.Sigla_Prov == entity.Sigla_Prov) != null)
                  result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Prov),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
              }
              else
              {
                if (RepoManager.Tab_ProvRepo.SingleOrDefault(u => u.Sigla_Prov == entity.Sigla_Prov && u.Tab_Prov_Id != entity.Tab_Prov_Id) != null)
                  result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Prov_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
              }
            }
           //
           //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
          //
       
          if (CommonService.Nz(entity.Descrizione_Prov, "") == "")
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Prov),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_PROV));     
          //
          //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
          //
          //
          //4) verifico, per una serie di campi, che il valore del campo sia corretto
          //
          // NESSUN CONTROLLO DI QUESTO TIPO
          //
          //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
          //
          if (CommonService.Nz(entity.Sigla_Prov, "") != "")
            if (entity.Sigla_Prov.Length > 2)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Prov),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
              PowerWebResources.FLD_SIGLA_PROV, PowerWebResources.VALORE_2 ));
          if (CommonService.Nz(entity.Descrizione_Prov, "") != "")
            if (entity.Descrizione_Prov.Length > 50)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Prov),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
              PowerWebResources.FLD_DESCRIZIONE_PROV, PowerWebResources.VALORE_50 ));
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
            var CodErr = "Tab_Prov_Id: " + entity.Tab_Prov_Id;
            throw ex;
        }
        return result;
    }
  }
}
