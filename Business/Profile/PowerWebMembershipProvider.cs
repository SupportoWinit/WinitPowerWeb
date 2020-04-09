using Business.DataClasses;
using Business.Repository;
using Common;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Security;

namespace Business.Profile
{

    //Quando possibile ripensare a questa classe o gestire l'autenticazione in maniera differente e più dettagliata
    //senza utilizzare metodi non congrui (noi non utilizziamo in alcun modo MembershipUser bensi' User)

    public class PowerWebMembershipProvider : MembershipProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PowerWebMembershipProvider));

        private string _appName;
        private string _connectionStringName;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private int _maxInvalidPasswordAttempts;
        private int _minRequiredNonalphanumericCharacters = 0;
        private int _minRequiredPasswordLength = 0;
        private int _passwordAttemptWindow;
        private string _passwordStrengthRegularExpression;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            _appName = config["applicationName"];
            _connectionStringName = config["connectionStringName"];
            _enablePasswordReset = false;
            _enablePasswordRetrieval = false;
            _requiresQuestionAndAnswer = false;
            _requiresUniqueEmail = false;
            _maxInvalidPasswordAttempts = 5;
            _minRequiredPasswordLength = int.Parse(config["minRequiredPasswordLength"]);
            _passwordAttemptWindow = 10;
            _passwordStrengthRegularExpression = config["passwordStrengthRegularExpression"];
        }



        public override MembershipUser GetUser(string userName, bool userIsOnline)
        {
            Utenti currentUser = RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == userName);

            if (currentUser == null)
                return null;
            return new MembershipUser(this.Name, currentUser.Codice_Utente, currentUser.Utenti_Id, String.Empty, null, null, currentUser.Disabilitazione_Utente, false, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow);
        }



        /// <summary>
        /// Inizializza il nuovo utente nella sessione corrente (viene utilizzata una volta effettuato il login)
        /// </summary>
        /// <param name="currentUser">L'utente da loggare.</param>
        public static void InitializeUser(Utenti currentUser)
        {
            PowerWebContext.Current.User = currentUser;

            PowerWebContext.Current.Lingua = currentUser.Lingue;

            PowerWebContext.Current.Versione = currentUser.Versioni;

            RepoManager.ResourcesRepo.ResetResourcesDictionary();

            RepoManager.ParamRepo.ResetParametersRow();

            ResetDomainFilter(currentUser);

            PowerWebContext.Current.TabAuts = InitUserTabAuts();

            PowerWebContext.Current.TabFunzs = InitUserTabFunzs();

            _log.InfoFormat("Utente {0} correttamente loggato.",
                currentUser.Codice_Utente
                );
        }
        public static void InitializeUser(Utenti currentUser, Utenti superUser = null)
        {
            InitializeUser(currentUser);

            if (superUser != null)
                PowerWebContext.Current.IsSupervised = true;
        }

        /// <summary>
        /// Crea un utente amministratore dato username e password.
        /// Viene creata una salt key e creato inoltre il relativo
        /// record tab_Aut
        /// </summary>
        /// <param name="userName">Username.</param>
        /// <param name="password">Password.</param>
        /// <returns></returns>
        public static bool CreateAdminUser(string userName, string password, string question, string answer)
        {
            bool isValid = true;

            try
            {
                string salt = BusinessService.CreateSalt(8);

                Utenti newUser = RepoManager.UtentiRepo.Init();
                RepoManager.UtentiRepo.SetEntityBeforeAddOrUpdate(newUser);

                newUser.Codice_Utente = userName;
                newUser.PasswordHash_Utente = BusinessService.Encrypt(password, newUser.SaltKey_Utente);
                newUser.DataOraUltimaModifica_Utente = DateTime.Now;
                newUser.Data_Registrazione_Utente = DateTime.Now;
                newUser.N_GG_Val_Psw_Utente = RepoManager.ParamRepo.ParametersRow.PasswordExpirationDays ?? null;
                newUser.Versioni_Id = 3;
                newUser.DataUltimoAgg_Psw_Utente = DateTime.Now;
                newUser.SecretQuestion = question;
                newUser.SecretAnswer = answer;

                RepoManager.UtentiRepo.Add(newUser, true);

                //Quando si INSERISCE un NUOVO UTENTE in Tabella Uteneti viene creato AUTOMATICAMENTE anche il corrispondente record in TAB_AUT per il nuovo Utente
                Tab_Aut newTabAut = RepoManager.Tab_AutRepo.Init();
                newTabAut.Del_Aut = newTabAut.Funz_Aut = newTabAut.Ins_Aut = newTabAut.Mod_Aut = 10;
                newTabAut.Utenti = newUser;
                RepoManager.Tab_AutRepo.Add(newTabAut, true);


            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la creazione di un utente admin a causa dell'exception {0}", ex.Message);

                isValid = !isValid;
            }

            return isValid;
        }

        /// <summary>
        /// Controllo data di scadenza password dato un certo
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Utente inesistente o duplicato!</exception>
        public static LoginValidationResult CheckUserPassWordExpiration(string userName, bool init = true)
        {
            LoginValidationResult result = new LoginValidationResult() { isValid = true };

            Utenti user = RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == userName);

            if (user == null)
                throw new InvalidOperationException("Utente inesistente o duplicato!");

            if (RepoManager.ParamRepo.ParametersRow.Abilita_Privacy)
            {
                DateTime? expirationDate = GetUserPasswordExpirationDate(user);

                if (expirationDate != null)
                {
                    bool isPassWordExpired = DateTime.Now >= expirationDate.Value;

                    if (isPassWordExpired && RepoManager.ParamRepo.ParametersRow.BlockLoginOnUserPswExpired)
                    {
                        result.isValid = false;
                        result.validationError = "Attenzione! La password utente ha superato la data di validità. E' necessario modificarla per poter effettuare l'accesso!";

                        if (String.IsNullOrEmpty(user.SecretQuestion))
                            result.completeSecretQuestion = true;
                    }
                    else if (user.ChangePasswordOnLogin)
                    {
                        result.isValid = false;
                        result.validationError = "Attenzione! La password utente ha superato la data di validità. E' necessario modificarla per poter effettuare l'accesso!";
                    }
                    else if (isPassWordExpired && !RepoManager.ParamRepo.ParametersRow.BlockLoginOnUserPswExpired)
                    {
                        result.validationError = "<br>Attenzione! La password utente ha superato la data di validità.</br><br>Vi consigliamo di modificarla al prossimo accesso!</br>";
                        result.isValid = true;
                    }
                    else if (expirationDate.Value.AddDays(-user.ChangePasswordWarningDays) < DateTime.Now)
                    {
                        result.validationError = "<br>Attenzione! La password utente si sta avvicinando alla scadenza.</br><br>Vi consigliamo di modificarla al prossimo accesso!</br>";
                        result.isValid = true;
                    }

                    if (user.Col_Id != null)
                    {
                        DateTime? dataLicenziamento = user.Col.Data_Disponibilita_Fine_Col;

                        if (dataLicenziamento != null && dataLicenziamento <= DateTime.Now)
                        {
                            result.validationError = "Attenzione! Accesso non consentito per data fine disponibilità del collaboratore inferiore alla data odierna!";
                            result.fired = true;
                            result.isValid = false;
                        }
                    }
                }
            }

            if (result.isValid && init)
            {
                InitializeUser(user);
            }

            return result;
        }
        public static bool UpdatePassWord(string userName, string newPassword, ref string error)
        {
            var utente = RepoManager.UtentiRepo.SingleOrDefault(user => user.Codice_Utente == userName);

            if (utente == null)
            {
                _log.ErrorFormat("Errore durante la modifica della password per l'utente {0}. L'utente non è stato trovato", userName);

                error = "Errore interno. L'utente non è stato trovato! Password non modificata!";

                return false;
            }

            utente.PasswordHash_Utente = BusinessService.Encrypt(newPassword, utente.SaltKey_Utente);
            utente.DataUltimoAgg_Psw_Utente = DateTime.Now;

            if (utente.ChangePasswordOnLogin)
                utente.ChangePasswordOnLogin = false;

            Utenti_History oldHistory = RepoManager.Utenti_HistoryRepo.DbSet
                                                                      .Where(his => his.Utenti_Id == utente.Utenti_Id)
                                                                      .OrderByDescending(u => u.Data_Old_Change_Psw_Utenti_History)
                                                                      .FirstOrDefault();

            Utenti_History userHistory = new Utenti_History()
            {
                Data_Change_Psw_Utenti_History = DateTime.Now,
                Data_Old_Change_Psw_Utenti_History = oldHistory != null ? oldHistory.Data_Change_Psw_Utenti_History : DateTime.Now,
                Utenti_Id = utente.Utenti_Id,
                Salt_Key_Utenti_History = utente.SaltKey_Utente,
                Password_Hash_Utenti_History = utente.PasswordHash_Utente
            };

            RepoManager.Utenti_HistoryRepo.Add(userHistory);

            try
            {
                RepoManager.UtentiRepo.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _log.ErrorFormat("Errore durante il salvataggio della nuova entità {0} per l'utente {1} con exception {2}", nameof(Utenti_History), userName, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore generico durante l'inserimento di una nuova entità di tipo {0} per l'utente {1} con exception {2}", nameof(Utenti_History), userName, ex.Message);
                throw;
            }

            return true;
        }
        public static void ResetDomainFilter(Utenti currentUser)
        {
            PowerWebContext.Current.DomainFilter = DomainFilterEnum.None;

            PowerWebContext.Current.Resps = RepoManager.RespRepo.GetAll().ToList();

            PowerWebContext.Current.Fils = RepoManager.FilRepo.GetAll().ToList();

            PowerWebContext.Current.DomainFilter = (DomainFilterEnum)RepoManager.ParamRepo.ParametersRow.DomainFilter;

            if (PowerWebContext.Current.DomainFilter != DomainFilterEnum.None)
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp)
                {
                    if (currentUser.Resp_Inclusive)
                    {
                        if (currentUser.Utenti_Resp.Count > 0)
                        {
                            PowerWebContext.Current.Resps = new List<Resp>();

                            foreach (Utenti_Resp utenti_Resp in currentUser.Utenti_Resp)
                                PowerWebContext.Current.Resps.Add(utenti_Resp.Resp);
                        }
                    }
                    else
                    {
                        if (currentUser.Utenti_Resp.Count > 0)
                        {
                            var excludedRespIds = currentUser.Utenti_Resp.Select(resp => resp.Resp_Id);
                            var resps = RepoManager.RespRepo.Find(resp => !excludedRespIds.Contains(resp.Resp_Id));
                            PowerWebContext.Current.Resps = resps.ToList();

                            foreach (Utenti_Resp utenti_Resp in currentUser.Utenti_Resp)
                            {
                                if (utenti_Resp.Dominio_Utenti_Resp == (int)DomainEnum.ViewUpdate)
                                    PowerWebContext.Current.Resps.Add(utenti_Resp.Resp);
                            }
                        }
                        else PowerWebContext.Current.Resps = new List<Resp>();
                    }
                }

                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil)
                {
                    if (currentUser.Fil_Inclusive)
                    {
                        if (currentUser.Utenti_Fil.Count > 0)
                        {
                            PowerWebContext.Current.Fils = new List<Fil>();

                            foreach (Utenti_Fil utenti_Fil in currentUser.Utenti_Fil)
                                PowerWebContext.Current.Fils.Add(utenti_Fil.Fil);
                        }
                    }
                    else
                    {
                        if (currentUser.Utenti_Fil.Count > 0)
                        {
                            var excludedFilIds = currentUser.Utenti_Fil.Select(fil => fil.Fil_Id);
                            var fils = RepoManager.FilRepo.Find(fil => !excludedFilIds.Contains(fil.Fil_Id));
                            PowerWebContext.Current.Fils = fils.ToList();

                            foreach (Utenti_Fil utenti_Fil in currentUser.Utenti_Fil)
                            {
                                if (utenti_Fil.Dominio_Utenti_Fil == (int)DomainEnum.ViewUpdate)
                                    PowerWebContext.Current.Fils.Add(utenti_Fil.Fil);
                            }
                        }
                        else PowerWebContext.Current.Fils = new List<Fil>();
                    }
                }

                PowerWebContext.Current.CantsIds = RepoManager.CantRepo.DbSet.AsNoTracking().Where(RepoManager.CantRepo.Filter).Select(cant => cant.Cant_Id).ToList();
                PowerWebContext.Current.ColsIds = RepoManager.ColRepo.DbSet.AsNoTracking().Where(RepoManager.ColRepo.Filter).Select(col => col.Col_Id).ToList();

            }
        }
        public override bool ValidateUser(string userName, string password)
        {
            bool isValidUser = false;

            Utenti currentUser = RepoManager.UtentiRepo.GetAllEnabled().SingleOrDefault(u => u.Codice_Utente.ToUpper() == userName.ToUpper());

            if (currentUser != null)
            {
                isValidUser = BusinessService.Encrypt(password, currentUser.SaltKey_Utente) == currentUser.PasswordHash_Utente;
                if (isValidUser)
                {
                    InitializeUser(currentUser);

                }
                else
                    _log.Warn(String.Format("Log in failed by {0} ", userName));
            }

            return isValidUser;
        }
        public static LoginValidationResult ValidatePassword(string userName, string password, bool checkForUser = true)
        {
            LoginValidationResult result = new LoginValidationResult() { isValid = true };

            Utenti user = RepoManager.UtentiRepo.SingleOrDefault(us => us.Codice_Utente == userName);

            if (checkForUser && user == null)
            {
                result.isValid = false;
                result.validationError = "Attenzione! Utente inesistente!";

                return result;
            }

            if (password.Count() < Membership.MinRequiredPasswordLength)
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MAGGIORE_UGUALE_Y, "password", Membership.MinRequiredPasswordLength.ToString()));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non rispetta la lunghezza minima ({1})'", userName, Membership.MinRequiredPasswordLength);
            }

            if (!password.Any(c => char.IsNumber(c)))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_VALORI_NUMERICI, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non contiene valori numerici", userName);
            }

            if (!password.Any(c => char.IsLetter(c)))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_LETTERE, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non contiene alcuna lettera", userName);

            }

            if (password.Contains(" "))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_NON_DEVE_CONTENERE_CONTENERE_SPAZI, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non può contenere spazi", userName);
            }

            if (!password.Any(c => !Char.IsLetterOrDigit(c)))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_UN_CARATTERE_SPECIALE, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non contiene una lettera speciale", userName);

            }

            if (!password.Any(c => char.IsUpper(c)))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_CONTENERE_LETTERE_MAIUSCOLE, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non contiene lettere maiuscole", userName);

            }

            if (password.ToLower().Contains(userName.ToLower()))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_NON_DEVE_CONTENERE_CODICE_UTENTE, "password"));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} non può contenere il nome utente", userName);

            }


            if (checkForUser && user.Utenti_History.Any(p => p.Password_Hash_Utenti_History == BusinessService.Encrypt(password, user.SaltKey_Utente)))
            {
                result.AddFormErrors("newPassWord", BusinessService.GetLocalizedString(PowerWebResources.ERR_PASSWORD_GIA_UTILIZZATA));

                _log.InfoFormat("La password inserita durante il login da parte dell'utente {0} è già stata usata in precedenza", userName);
            }

            if (result.formErrors.Any())
            {
                result.isValid = false;
            }


            if (!result.isValid)
            {
                _log.InfoFormat("Login fallito per l'utente {0}", userName);
            }

            return result;
        }
        public static bool IsAdminUserMissing()
        {
            int adminLevel = Common.Properties.Settings.Default.Admin_Level;

            return !RepoManager.UtentiRepo.DbSet.ToList().Any(user => user.Liv_Utente == adminLevel);
        }
        public static bool IsUserDuplicated(string userName)
        {
            return RepoManager.UtentiRepo.DbSet.Any(user => user.Codice_Utente == userName);
        }
        public static string GetUserSecretQuestion(string userName)
        {
            return RepoManager.UtentiRepo.Single(user => user.Codice_Utente == userName).SecretQuestion;
        }
        public static string GetUserSecretAnswer(string userName)
        {
            return RepoManager.UtentiRepo.Single(user => user.Codice_Utente == userName).SecretAnswer;
        }

        public static void SetUserSecretQuestion(string userName, string secretQuestion)
        {
            var utente = RepoManager.UtentiRepo.Single(user => user.Codice_Utente == userName);

            utente.SecretQuestion = secretQuestion;

            RepoManager.SaveChanges();

        }
        public static void SetUserSecretAnswer(string userName, string secretAnswer)
        {
            var utente = RepoManager.UtentiRepo.Single(user => user.Codice_Utente == userName);

            utente.SecretAnswer = secretAnswer;

            RepoManager.SaveChanges();

        }
        private static List<Tab_Aut> InitUserTabAuts()
        {
            var currentAuths = RepoManager.Tab_AutRepo.Find(ta => ta.Utenti_Id == null || ta.Utenti_Id == PowerWebContext.Current.User.Utenti_Id, true).ToList();

            Dictionary<int?, Tab_Aut> filteredAuths = new Dictionary<int?, Tab_Aut>();

            foreach (var auth in currentAuths)
            {
                if (auth.Tab_Funz_Id == null)
                    auth.Tab_Funz_Id = -1;

                if (auth.Utenti_Id == null && !filteredAuths.ContainsKey(auth.Tab_Funz_Id))
                    filteredAuths.Add(auth.Tab_Funz_Id, auth);
                else
                {

                    if (filteredAuths.ContainsKey(auth.Tab_Funz_Id))
                        filteredAuths[auth.Tab_Funz_Id] = auth;
                    else
                        filteredAuths.Add(auth.Tab_Funz_Id, auth);
                }
            }

            var filteredAuthsList = filteredAuths.Values.ToList();
            foreach (var auth in filteredAuthsList)
                if (auth.Tab_Funz_Id == -1)
                    auth.Tab_Funz_Id = null;

            return filteredAuthsList;
        }
        private static List<Tab_Funz> InitUserTabFunzs()
        {
            var funzs = RepoManager.Tab_FunzRepo.GetAll(true).ToList();

            List<Tab_Funz> filteredFunzs = new List<Tab_Funz>();

            if (PowerWebContext.Current.UserLevel == null)
                filteredFunzs = funzs;
            else
            {
                #region Filter functions
                foreach (Tab_Funz funz in funzs)
                {
                    // Vengono abilitate TUTTE E SOLO 
                    // le Funzioni che NON sono state espressamente definite in TAB_AUT
                    // le Funzioni espressamente definite in TAB_AUT che hanno un Livello <= al livello dell'Utente
                    // ESCLUDENDO PERO' le eventuali Funzioni presenti in TAB_AUT ed abbinate a quell'Utente con un Livello MAGGIORE a quello dell'Utente
                    if (PowerWebContext.Current.TabAuts.Count(ta => ta.Tab_Funz_Id == funz.Tab_Funz_Id) > 0)
                    {
                        //Cerca le eventuali Autorizzazioni per Funz/Utente
                        //Se NON esiste una Autorizzazione specifica per quella Funzione/Utente
                        //Verifica se esiste una Autorizzazione specifica per la Sola Funzione (senza Utente)
                        Tab_Aut currentTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Tab_Funz_Id == funz.Tab_Funz_Id && ta.Utenti_Id == PowerWebContext.Current.User.Utenti_Id);
                        if (currentTabAut == null)
                            currentTabAut = PowerWebContext.Current.TabAuts.SingleOrDefault(ta => ta.Tab_Funz_Id == funz.Tab_Funz_Id && ta.Utenti_Id == null);
                        //sia che esistesse una Autorizzazione specifica per Funz/Utente o esistesse per la sola Funziona
                        //Abilita la Funzione e/o la Funz/Utente solo se ha un livello (Pari o Dispari) MINORE di quello Dell'Utente
                        if (PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.FuncAccess, currentTabAut, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum)))
                            filteredFunzs.Add(funz);
                    }
                    else if (PowerWebContext.Current.User.IsUserAutorized(Utenti.OperationTypeEnum.FuncAccess, null, RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DefaultFunzAuthLevelEnum)))
                        //Abilita TUTTE le FUNZIONI NON PRESENTI IN TAB_AUT
                        filteredFunzs.Add(funz);
                }
                #endregion
            }

            return filteredFunzs;
        }
        private static DateTime? GetUserPasswordExpirationDate(Utenti user)
        {
            if (!user.DataUltimoAgg_Psw_Utente.HasValue)
                return DateTime.MinValue; //Se non c'è una data di concessione ritorno data minima

            int pswExpirationDays = RepoManager.ParamRepo.ParametersRow.PasswordExpirationDays ?? 0;

            if (user.N_GG_Val_Psw_Utente.HasValue)
            {
                pswExpirationDays = (int)user.N_GG_Val_Psw_Utente;
            }

            return user.DataUltimoAgg_Psw_Utente.Value.AddDays(pswExpirationDays);

        }

        #region Properties

        public override string ApplicationName
        {
            get
            {
                return this._appName;
            }
            set
            {
                this._appName = value;
            }
        }

        public override bool EnablePasswordReset
        {
            get
            {
                return this._enablePasswordReset;
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get
            {
                return this._maxInvalidPasswordAttempts;
            }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get
            {
                return this._minRequiredNonalphanumericCharacters;
            }
        }

        public override int MinRequiredPasswordLength
        {
            get
            {
                return this._minRequiredPasswordLength;
            }
        }

        public override int PasswordAttemptWindow
        {
            get
            {
                return this._passwordAttemptWindow;
            }
        }

        public override string PasswordStrengthRegularExpression
        {
            get
            {
                return this._passwordStrengthRegularExpression;
            }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get
            {
                return this._requiresQuestionAndAnswer;
            }
        }

        public override bool RequiresUniqueEmail
        {
            get
            {
                return this._requiresUniqueEmail;
            }
        }

        public override bool EnablePasswordRetrieval
        {
            get
            {
                return this._enablePasswordRetrieval;
            }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get
            {
                return MembershipPasswordFormat.Hashed;
            }
        }

        #endregion

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            Utenti utente = RepoManager.UtentiRepo.Single(user => user.Codice_Utente == username);

            if (utente != null)
            {
                utente.PasswordHash_Utente = BusinessService.Encrypt(newPassword, utente.SaltKey_Utente);
                RepoManager.UtentiRepo.SaveChanges();
                return true;
            }

            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {


            Utenti user = RepoManager.UtentiRepo.SingleOrDefault(u => u.Codice_Utente == username, true);
            if (user == null)
                return null;

            return BusinessService.Decrypt(user.PasswordHash_Utente, user.SaltKey_Utente);


        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override string ResetPassword(string username, string answer)
        {
            return string.Empty;
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }
    }
}
