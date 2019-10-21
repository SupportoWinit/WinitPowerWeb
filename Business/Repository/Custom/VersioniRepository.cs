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
  public class VersioniRepository : GenericRepository<Versioni>, IVersioniRepository
  {
    public VersioniRepository(PowerWebEntities context)
      : base(context)
    {

    }
    //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
    private static DateTime DataOraRecord;

    private static void ResetSession()
    {
    }
    public override Versioni Init()
    {
        Versioni oNewRecord = base.Init();
        oNewRecord.DisAbilitazione_Versioni = false;
        oNewRecord.Data_Registrazione_Versioni  = DateTime.UtcNow;
        oNewRecord.DataOraUltimaModifica_Versioni  = DateTime.UtcNow;
        return oNewRecord;
    }
    public override void SetEntityBeforeAddOrUpdate(Versioni entity)
    {
        //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
        DataOraRecord = entity.DataOraUltimaModifica_Versioni;
        entity.DataOraUltimaModifica_Versioni = DateTime.UtcNow;
    }

    public override Dictionary<string, string> Check(Versioni entity, bool isNew = false, bool isResetSession = true)
    {
      Dictionary<string, string> result = new Dictionary<string, string>();
    //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
        if (isResetSession)
            ResetSession();

        try
        {
            //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo                
            if (!isNew)
            {
                DateTime DataOraRecordDb = RepoManager.VersioniRepo.Single(u => u.Versioni_Id == entity.Versioni_Id).DataOraUltimaModifica_Versioni;
                if (DataOraRecordDb > DataOraRecord)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Versioni),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_VERSIONI));
                }
            }     
          //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
          //
          if (CommonService.Nz(entity.Nome_Versioni, "") == "")
            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Versioni ),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_VERSIONI));
          else
          {
            if (isNew)
            {
                if (RepoManager.VersioniRepo.SingleOrDefault(u => u.Nome_Versioni == entity.Nome_Versioni) != null)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Versioni),
               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
            else
            {
                if (RepoManager.VersioniRepo.SingleOrDefault(u => u.Nome_Versioni == entity.Nome_Versioni && u.Versioni_Id != entity.Versioni_Id) != null)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Versioni),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
            }
          }
          // Verifico che l'Utente che sta modificando la Tabella abbia livello  WINIT
          if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Versioni_Id),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO_TABELLA)); 
          //
          //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
          //  

          if (CommonService.Nz(entity.Nome_Versioni, "") == "")
              result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Versioni),
              BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_VERSIONI));
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
            var CodErr = "Versioni_Id: " + entity.Versioni_Id;
            throw ex;
        }
        return result;
    }
  }
}
