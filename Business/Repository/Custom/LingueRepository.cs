using System;
using System.Collections.Generic;
using System.Linq;
using Business.Properties;
using Domain;
using Data;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class LingueRepository : GenericRepository<Lingue>, ILingueRepository
    {
        public LingueRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        private static void ResetSession()
        {            
        }

        public override Lingue Init()
        {
            Lingue oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Lingue = false;
            oNewRecord.Data_Registrazione_Lingue = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Lingue = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Lingue entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Lingue;
            entity.DataOraUltimaModifica_Lingue = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Lingue entity, bool isNew = false, bool isResetSession = true)
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
                    DateTime DataOraRecordDb = RepoManager.LingueRepo.Single(u => u.Lingue_Id == entity.Lingue_Id).DataOraUltimaModifica_Lingue;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Lingue),
                            BusinessService.GetLocalizedString(
                                PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE,
                                PowerWebResources.FLD_DATAORAULTIMAMODIFICA_LINGUE));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Sigla_Lingue))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Lingue),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_SIGLA_LINGUE));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.LingueRepo.SingleOrDefault(u => u.Sigla_Lingue == entity.Sigla_Lingue) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Lingue),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.LingueRepo.SingleOrDefault(u => u.Sigla_Lingue == entity.Sigla_Lingue && u.Lingue_Id != entity.Lingue_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Lingue),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }

                }
                // Verifico che l'Utente che sta modificando la Tabella abbia livello >=12
                if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                  result.AddOrAppend(CommonService.GetPropertyName(() => entity.Lingue_Id),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO_TABELLA));   
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (String.IsNullOrEmpty(entity.Nome_Lingue))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Lingue),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_LINGUE));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //            
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Sigla_Lingue, "") != "")
                    if (entity.Sigla_Lingue.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Sigla_Lingue),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_SIGLA_LINGUE, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Nome_Lingue, "") != "")
                    if (entity.Nome_Lingue.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Lingue),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_LINGUE, PowerWebResources.VALORE_50 ));
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
                var CodErr = "Lingue_Id: " + entity.Lingue_Id;
                throw ex;
            }
            return result;
        }
    }
}
