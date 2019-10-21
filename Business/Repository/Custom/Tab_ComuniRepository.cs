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
  public class Tab_ComuniRepository : GenericRepository<Tab_Comuni>, ITab_ComuniRepository
  {
    public Tab_ComuniRepository(PowerWebEntities context)
      : base(context)
    {
    }

    private static void ResetSession()
        {
        }
    public override Dictionary<string, string> Check(Tab_Comuni entity, bool isNew = false, bool isResetSession = true)
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
          if (String.IsNullOrEmpty(entity.Luogo_Tab_Comuni) ||
              String.IsNullOrEmpty(entity.Cap_Tab_Comuni))
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Comuni_Id),
            BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
          else
          {
            if (isNew)
            {
                //Viene controllato che NON si inserisca un Comune/Cap già esistente
              if (RepoManager.Tab_ComuniRepo.SingleOrDefault(u => u.Luogo_Tab_Comuni == entity.Luogo_Tab_Comuni && 
                                                                  u.Cap_Tab_Comuni == entity.Cap_Tab_Comuni ) != null)           
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Comuni_Id),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
            else
            {
              if (RepoManager.Tab_ComuniRepo.SingleOrDefault(u => u.Luogo_Tab_Comuni == entity.Luogo_Tab_Comuni &&
                                                                  u.Cap_Tab_Comuni == entity.Cap_Tab_Comuni &&
                                                                  u.Tab_Comuni_Id != entity.Tab_Comuni_Id) != null)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Comuni_Id),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
          }
          //
          //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
          //
          if (CommonService.Nz(entity.Tab_Prov_Id, 0) == 0)
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Prov_Id),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
              PowerWebResources.FLD_TAB_PROV_ID));
          else
          {
            if (RepoManager.Tab_ProvRepo.SingleOrDefault(u => u.Tab_Prov_Id == entity.Tab_Prov_Id) == null)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Prov_Id),
                BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                PowerWebResources.FLD_TAB_PROV_ID, PowerWebResources.STR_TAB_PROV));
          }
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
          if (CommonService.Nz(entity.Cap_Tab_Comuni, "") != "")
            if (entity.Cap_Tab_Comuni.Length > 50)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cap_Tab_Comuni),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
              PowerWebResources.FLD_CAP_TAB_COMUNI, PowerWebResources.VALORE_50 ));
          if (CommonService.Nz(entity.Luogo_Tab_Comuni, "") != "")
            if (entity.Luogo_Tab_Comuni.Length > 50)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Luogo_Tab_Comuni),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
              PowerWebResources.FLD_LUOGO_TAB_COMUNI, PowerWebResources.VALORE_50 ));
          if (CommonService.Nz(entity.Codice_Luogo_Tab_Comuni, "") != "")
            if (entity.Codice_Luogo_Tab_Comuni.Length > 4)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Luogo_Tab_Comuni),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
              PowerWebResources.FLD_CODICE_LUOGO_TAB_COMUNI, PowerWebResources.VALORE_4 ));      
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
            var CodErr = "Tab_Comuni_Id: " + entity.Tab_Comuni_Id;
            throw ex;
        }
        return result;
    }
  }
}
