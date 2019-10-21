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
    public class Utenti_RespRepository : GenericRepository<Utenti_Resp>, IUtenti_RespRepository
    {
        public Utenti_RespRepository(PowerWebEntities context)
            : base(context)
        {
        }

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Utenti> Utentis
        {
            get
            {
              List<Utenti> oLista = PowerWebContext.GetFromSession<List<Utenti>>("Utentis_Utenti_RespRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.UtentiRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Utenti>>("Utentis_Utenti_RespRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Resp> Resps
        {
            get
            {
              List<Resp> oLista = PowerWebContext.GetFromSession<List<Resp>>("Resps_Utenti_RespRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.RespRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Resp>>("Resps_Utenti_RespRepo", oLista);
                }
                return oLista;
            }
        }
        
        private static void ResetSession()
        {
          PowerWebContext.SetToSession<List<Utenti>>("Utentis_Utenti_RespRepo", null);
          PowerWebContext.SetToSession<List<Fil>>("Resps_Utenti_RespRepo", null);
        }

        public override Utenti_Resp Init()
        {
            Utenti_Resp oNewRecord = base.Init();
            //oNewRecord.DisAbilitazione_Pru_Col = false;
            //oNewRecord.Data_Registrazione_Pru_Col = DateTime.UtcNow;
            //oNewRecord.DataOraUltimaModifica_Pru_Col = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Utenti_Resp entity)
        {          
        }

        public override Dictionary<string, string> Check(Utenti_Resp entity, bool isNew = false, bool isResetSession = true)
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
                //
                //  CHAIVE UNIVOCA : Utente_Id + Fil_ID
                // 
                if (CommonService.Nz(entity.Utenti_Id, 0) == 0 ||
                    CommonService.Nz(entity.Resp_Id, 0) == 0 )
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Resp_Id),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                      if (RepoManager.Utenti_RespRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id &&
                           u.Resp_Id == entity.Resp_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Resp_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                      if (RepoManager.Utenti_RespRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id &&
                          u.Resp_Id == entity.Resp_Id && u.Utenti_Resp_Id != entity.Utenti_Resp_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Resp_Id ),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //            
                if (entity.Utenti_Id == 0)
                  result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_UTENTI_ID));
                else
                {
                  if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Utenti_Id == entity.Utenti_Id) == null)
                      result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_UTENTI_ID, PowerWebResources.STR_UTENTI));
                }
                if (entity.Resp_Id == 0)
                  result.AddOrAppend(CommonService.GetPropertyName(() => entity.Resp_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_RESP_ID));
                else
                {
                  if (RepoManager.RespRepo.SingleOrDefault(u => u.Resp_Id == entity.Resp_Id) == null)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Resp_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_RESP_ID, PowerWebResources.STR_RESP));
                }
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
                var CodErr = "Utenti_Resp_Id: " + entity.Utenti_Resp_Id;
                throw ex;
            }
            return result;
        }         
    }
}
