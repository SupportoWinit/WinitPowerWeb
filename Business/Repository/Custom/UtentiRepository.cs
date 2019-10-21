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
using System.Data.Entity.SqlServer;
using System.Data.Entity;

namespace Business.Repository.Custom
{
    public class UtentiRepository : GenericRepository<Utenti>, IUtentiRepository
    {
        public UtentiRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static void ResetSession()
        {
        }
        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;
        public override Utenti Init()
        {
            Utenti newUser = base.Init();
            newUser.Disabilitazione_Utente = false;
            newUser.Lingue_Id = RepoManager.LingueRepo.First().Lingue_Id;
            newUser.Data_Registrazione_Utente = DateTime.UtcNow;
            newUser.DataOraUltimaModifica_Utente = DateTime.UtcNow;
            newUser.Menu_Tipo_Id = RepoManager.Menu_TipoRepo.First().Menu_Tipo_Id;
            return newUser;
        }
        public override void SetEntityBeforeAddOrUpdate(Utenti entity)
        {
            //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Utente;
            entity.DataOraUltimaModifica_Utente = DateTime.UtcNow;
            entity.SaltKey_Utente = BusinessService.CreateSalt(8);
            if (entity.Password != null)
                entity.PasswordHash_Utente = BusinessService.Encrypt(entity.Password, entity.SaltKey_Utente);
        }
        public override Dictionary<string, string> Check(Utenti entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //Leggo la DataOraUltimaModifica ATTUALE dal Record del DB per verificare che nessuno abbia modificato il Record nel frattempo
                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.UtentiRepo.Single(u => u.Utenti_Id == entity.Utenti_Id).DataOraUltimaModifica_Utente;

                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Utente),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_UTENTE));
                    }
                }

                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Utente))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Utente),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_UTENTE));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == entity.Codice_Utente) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Utente),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == entity.Codice_Utente && u.Utenti_Id != entity.Utenti_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Utente),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (String.IsNullOrEmpty(entity.Password))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_PWD_UTENTE));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                // NESSUN CONTROLLO DI QUESTO TIPO DA FARE
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //             
                //Leggo i Dati attualmente nel DB (prima di essere modificati)
                //
                Utenti currentUtente = null;
                if (!isNew)
                    currentUtente = RepoManager.UtentiRepo.Single(u => u.Utenti_Id == entity.Utenti_Id);

                if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Admin_Level)
                //Un utente con Livello inferiore ad ADMIN (10) può modificare SOLO SE STESSO E NON PUO' CAMBIARSI IL LIVELLO
                {
                    if (entity.Utenti_Id != PowerWebContext.Current.User.Utenti_Id)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Utenti_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_ADMIN_NON_PUO_MOD_ALTRI_UTENTI));

                    if (!isNew)
                        if (currentUtente.Liv_Utente != entity.Liv_Utente_Edit)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Liv_Utente_Edit),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_LIVELLO_NON_MODIFICABILE_DA_UTENTE_NON_ADMIN));
                }



                if (PowerWebContext.Current.UserLevel.Funz_Aut == Common.Properties.Settings.Default.Admin_Level)
                //Un utente ADMIN (liv 10) può modificare anche un ALTRO UTENTE MA SOLO FINO AL LIVELLO 10 MA NON UN UTENTE CON LIVELLO >10
                {
                    if (!isNew)
                    // SE non è NUOVO verifico che l'Utente corrente non abbia un Livello > 10
                    {
                        if (currentUtente.Liv_Utente > Common.Properties.Settings.Default.Admin_Level)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Liv_Utente_Edit),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_LIVELLO_NON_MODIFICABILE_DA_UTENTE_NON_ADMIN));
                    }
                    if (entity.Liv_Utente_Edit < 0 || entity.Liv_Utente_Edit > Common.Properties.Settings.Default.Admin_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Liv_Utente_Edit),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_COMPRESO_FRA_Y_E_Z,
                            PowerWebResources.FLD_LIV_UTENTE, PowerWebResources.VALORE_0, PowerWebResources.VALORE_10));
                }
                if (PowerWebContext.Current.UserLevel.Funz_Aut > Common.Properties.Settings.Default.Admin_Level)
                //Un utenteWINIT (liv > 10) può modificare anche un ALTRO UTENTE MA SOLO FINO AL LIVELLO 12
                {
                    if (entity.Liv_Utente_Edit < 0 || entity.Liv_Utente_Edit > Common.Properties.Settings.Default.Winit_Level)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Liv_Utente_Edit),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_MODIFICA_RISERVATA_ALLA_WINIT));
                }

                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Utente, "") != "")
                    if (entity.Codice_Utente.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Utente),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_UTENTE, PowerWebResources.VALORE_50));
                //
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //
                if (CommonService.Nz(entity.Lingue_Id, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Lingue_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_LINGUE_ID));
                else
                    if (RepoManager.LingueRepo.SingleOrDefault(lng => lng.Lingue_Id == entity.Lingue_Id) == null)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Lingue_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                        PowerWebResources.FLD_LINGUA_ID, PowerWebResources.STR_LINGUE));
                }
                if (CommonService.Nz(entity.Menu_Tipo_Id, 0) == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Menu_Tipo_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOMEMENU_UTENTE));
                else
                    if (RepoManager.Menu_TipoRepo.SingleOrDefault(mnu => mnu.Menu_Tipo_Id == entity.Menu_Tipo_Id) == null)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Menu_Tipo_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                        PowerWebResources.FLD_NOMEMENU_UTENTE, PowerWebResources.STR_MENU));
                }

                // non può essere impostato contempraneamente il filtro utente per collaboratore e per cliente
                if (entity.Col_Id.HasValue && entity.Cli_Id.HasValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_FILTRI_COL_E_CLI_ASSIEME));

                //Domanda segreta
                if (String.IsNullOrEmpty(entity.SecretQuestion) && PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level && PowerWebContext.Current.UserLevel.Funz_Aut <= Common.Properties.Settings.Default.Winit_Level)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.SecretQuestion), BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_SECRETQUESTION));

                if (String.IsNullOrEmpty(entity.SecretAnswer) && PowerWebContext.Current.UserLevel.Funz_Aut >= Common.Properties.Settings.Default.Admin_Level && PowerWebContext.Current.UserLevel.Funz_Aut <= Common.Properties.Settings.Default.Winit_Level)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.SecretAnswer), BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_SECRETANSWER));

                //Controllo se la stringa contiene lettere
                if (!entity.Password.Any(c => char.IsLetter(c)))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_LETTERE, CommonService.GetPropertyName(() => entity.Password)));
                }

                //Controllo se la stringa contiene numeri
                if (!entity.Password.Any(c => char.IsNumber(c)))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_VALORI_NUMERICI, CommonService.GetPropertyName(() => entity.Password)));
                }

                //Controllo se la stringa contiene lettere maiuscole
                if (!entity.Password.Any(c => char.IsUpper(c)))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_LETTERE_MAIUSCOLE, CommonService.GetPropertyName(() => entity.Password)));
                }

                //Controllo la presenza di spaziatura
                if (entity.Password.Contains(" "))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_NON_DEVE_CONTENERE_CONTENERE_SPAZI, CommonService.GetPropertyName(() => entity.Password)));
                }

                //Controllo se manca almeno un carattere speciale
                if (!entity.Password.Any(c => !Char.IsLetterOrDigit(c)))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_UN_CARATTERE_SPECIALE, CommonService.GetPropertyName(() => entity.Password)));
                }

                //Controllo la lunghezza della password
                if (entity.Password.Count() < 8)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y, CommonService.GetPropertyName(() => entity.Password), "8"));
                }

                //Controllo che la password non contenga il codice utente
                if (entity.Password.ToLower().Contains(entity.Codice_Utente.ToLower()))
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Password), BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_NON_DEVE_CONTENERE_CODICE_UTENTE, CommonService.GetPropertyName(() => entity.Password)));
                }
                
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Utente_Id: " + entity.Utenti_Id;
                throw ex;
            }
            return result;
        }
        public IQueryable<Utenti> GetAllEnabled()
        {
            return Find(u => u.Disabilitazione_Utente == false).AsQueryable();
        }

        public List<Utenti> GetAllExpired()
        {
            return null;
        }

        public List<Utenti> GetAllExpiring(int addDays)
        {
            DateTime expirationDate = DateTime.Now.AddDays(addDays);

            List<Utenti> expiringUsers = DbSet.AsNoTracking().Where(u => DbFunctions.AddDays(u.DataUltimoAgg_Psw_Utente, u.N_GG_Val_Psw_Utente) < expirationDate && (DbFunctions.AddDays(u.DataUltimoAgg_Psw_Utente, u.N_GG_Val_Psw_Utente) > DateTime.Now)).ToList();

            return expiringUsers;
        }

        public Utenti GetWinitUser()
        {
            return FirstOrDefault(user => user.Codice_Utente == "WINIT");
        }

    }
}