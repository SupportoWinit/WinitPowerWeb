using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;

namespace Business.Repository.Custom
{
    public class Tab_AutRepository : GenericRepository<Tab_Aut>, ITab_AutRepository
    {
        public Tab_AutRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Aut>>("Tab_Auts_Repo", null);
        }

        public override Tab_Aut Init()
        {
            Tab_Aut oNewRecord = base.Init();
            oNewRecord.Data_Registrazione_Aut = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Aut = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Tab_Aut entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Aut;
            entity.DataOraUltimaModifica_Aut = DateTime.UtcNow;
        }

        public override Dictionary<string, string> Check(Tab_Aut entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession(); // TODO: attualmente, almeno in questo repository, non utilizzata, da verificare il da farsi
            try
            {
                //Leggo la DataOraUltimaModifica ATTUALE dal Record del DB per verificare che nessuno abbia modificato il Record nel frattempo
                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.Tab_AutRepo.Single(u => u.Aut_Id == entity.Aut_Id).DataOraUltimaModifica_Aut;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Aut),
                            BusinessService.GetLocalizedString(
                                PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE,
                                PowerWebResources.FLD_DATAORAULTIMAMODIFICA_AUT));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (CommonService.Nz(entity.Tab_Funz_Id, 0) == 0 && CommonService.Nz(entity.Utenti_Id, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_ALMENO_UN_CAMPO_TRA_X_E_Y_E_OBBLIGATORIO,
                      PowerWebResources.FLD_CODICE_UTENTE, PowerWebResources.FLD_CODICE_FUNZ));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_AutRepo.SingleOrDefault(u => u.Tab_Funz_Id == entity.Tab_Funz_Id &&
                           u.Utenti_Id == entity.Utenti_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                        //NON è possibile INSERIRE un NUOVO UTENTE perchè il Record Utente viene INSERITO AUTOMATICAMENTE alla Creazione del REcord UTENTE
                        if (CommonService.Nz(entity.Tab_Funz_Id, 0) == 0 && CommonService.Nz(entity.Utenti_Id, 0) != 0)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_UTENTE_NON_INSERIBILE_SENZA_CODICE_FUNZIONE));
                    }
                    else
                    {
                        if (RepoManager.Tab_AutRepo.SingleOrDefault(u => u.Tab_Funz_Id == entity.Tab_Funz_Id &&
                          u.Tab_Funz_Id != entity.Tab_Funz_Id && u.Aut_Id != entity.Aut_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }

                // Verifico che il livello utente sia maggiore o uguale del livello funzione generale, altrimenti il livello della funzione non è modificabile
                if (isNew) // se si sta processando un record nuovo
                {
                    // non posso inserire un record funzione o un record utente/funzione che abbia il livello di autorizzazione
                    // maggiore di quello dell'utente corrente
                    if (PowerWebContext.Current.UserLevel.Funz_Aut < entity.Funz_Aut)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO));
                }
                else // se si sta modificando un record
                {
                    // recupero il record prima della modifica
                    Tab_Aut precAut = RepoManager.Tab_AutRepo.SingleOrDefault(tbaut => tbaut.Aut_Id == entity.Aut_Id);

                    // non posso modificare un record funzione o un record utente/funzione che abbia il livello di autorizzazione
                    // maggiore di quello dell'utente corrente
                    if (PowerWebContext.Current.UserLevel.Funz_Aut < precAut.Funz_Aut)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO));
                }

                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                // 
                if (CommonService.Nz(entity.Tab_Funz_Id, 0) != 0)
                    if (RepoManager.Tab_FunzRepo.SingleOrDefault(u => u.Tab_Funz_Id == entity.Tab_Funz_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Funz_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_TAB_FUNZ_ID, PowerWebResources.STR_TAB_FUNZ));
                if (CommonService.Nz(entity.Utenti_Id, 0) != 0)
                    if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_UTENTI_ID, PowerWebResources.STR_UTENTI));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                if (CommonService.Nz(entity.Funz_Aut, 0) != 0)
                    if (entity.Funz_Aut > 12)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Funz_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_FUNZ_AUT, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Ins_Aut, 0) != 0)
                    if (entity.Ins_Aut > 12)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ins_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_INS_AUT, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Mod_Aut, 0) != 0)
                    if (entity.Mod_Aut > 12)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Mod_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_MOD_AUT, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.Del_Aut, 0) != 0)
                    if (entity.Del_Aut > 12)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Del_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           PowerWebResources.FLD_DEL_AUT, PowerWebResources.VALORE_10));
                if (CommonService.Nz(entity.MsgIns1_Aut, 0) != 0)
                    if (entity.MsgIns1_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgIns1_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGINS1_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.MsgIns2_Aut, 0) != 0)
                    if (entity.MsgIns2_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgIns2_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGINS2_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.MsgMod1_Aut, 0) != 0)
                    if (entity.MsgMod1_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgMod1_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGMOD1_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.MsgMod2_Aut, 0) != 0)
                    if (entity.MsgMod2_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgMod2_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGMOD2_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.MsgDel1_Aut, 0) != 0)
                    if (entity.MsgDel1_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgDel1_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGDEL1_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
                if (CommonService.Nz(entity.MsgDel2_Aut, 0) != 0)
                    if (entity.MsgDel2_Aut > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.MsgDel2_Aut),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                           PowerWebResources.FLD_MSGDEL2_AUT, PowerWebResources.VALORE_0, PowerWebResources.VALORE_1));
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
                var CodErr = "Tab_Aut_Id: " + entity.Aut_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckBeforeDelete(Tab_Aut entity)
        //Routine che gestisce le Check Specifica per i Soli casi di DELETE
        //Un Record UTENTE con Funz_Id = Null NON è MAI cancellabile
        //Un record con livello autorizzazione generale inferiore al livello dell'utente che lo sta cancellando non è cancellabile
        {
            Dictionary<string, string> result = new Dictionary<string, string>();


            if (entity.Utenti_Id != null && entity.Tab_Funz_Id == null)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_NON_CANCELLABILE));

            if(PowerWebContext.Current.UserLevel.Funz_Aut < entity.Funz_Aut)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO));

            return result;
        }
    }
}
