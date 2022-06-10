using Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Security;

namespace Domain
{
    //Classe che si occupa di referenziare tutte le informazioni riguardanti la session di un utente 
    //all'interno di powerWeb (solo dopo che si è effettuato il login).
    //la classe viene utilizzata come una singleton all'interno della sessione corrente (nella session ven'è una
    //sola inquanto ad una session corrisponde un solo utente ).
    //La classe è stata creata per tracciare e recuperare informazioni sull'utente attualmente loggato con un certo
    //profilo e legare a quest'ultimo le informazioni necessarie
    //La classe viene rimossa dalla session quando la session scade o quando viene effettuato il logout

    public class PowerWebContext
    {
        private Tab_Aut _userLevel;

        public static readonly ILog _log = LogManager.GetLogger(typeof(PowerWebContext));
        public static PowerWebContext Current
        {
            get
            {
                if (HttpContext.Current == null)
                {
                    object data = Thread.GetData(Thread.GetNamedDataSlot("PowerWebContext"));
                    if (data != null)
                    {
                        return (PowerWebContext)data;
                    }
                    PowerWebContext context = new PowerWebContext();

                    Thread.SetData(Thread.GetNamedDataSlot("PowerWebContext"), context);
                    return context;
                }
                if (GetFromSession<PowerWebContext>("PowerWebContext") == null)
                {
                    PowerWebContext context = new PowerWebContext();
                    if (HttpContext.Current.Session != null)
                        SetToSession("PowerWebContext", context);
                    return context;
                }
                return (PowerWebContext)HttpContext.Current.Session["PowerWebContext"];
            }



        }

        public bool IsSupervised { get; set; } = false;
        public Utenti User { get; set; }
        public Lingue Lingua { get; set; }
        public Versioni Versione { get; set; }
        public List<Fil> Fils { get; set; }
        public List<Resp> Resps { get; set; }
        public List<int> ColsIds { get; set; }
        public List<int> CantsIds { get; set; }
        public List<int> ClisIds { get; set; }
        public CultureInfo UserCultureInfo { get { return new CultureInfo(Lingua.Sigla_Lingue); } }



        /// <summary>
        /// Reupera dalla Tab_Auto l'elenco delle autorizzazioni relative alle sole funzioni compatibili con il livello dell'utente.
        /// </summary>
        /// <value>
        /// L'elenco delle autorizzazioni relative alle sole funzioni compatibili con il livello dell'utente.
        /// </value>
        public List<Tab_Aut> TabAuts { get; set; }

        /// <summary>
        /// Recupera l'elenco delle funzioni utilizzabili dall'utente (o perché non hanno autorizzazione specifica o perché hanno un livello di autorizzazione compatibile con quello utente).
        /// </summary>
        /// <value>
        /// L'elenco delle funzioni utilizzabili dall'utente (o perché non hanno autorizzazione specifica o perché hanno un livello di autorizzazione compatibile con quello utente).
        /// </value>
        public List<Tab_Funz> TabFunzs { get; set; }


        /// <summary>
        /// Recupera o setta il l'enum riguardante il dominio filiale/respons dell 'utente loggato al momento
        /// </summary>
        public DomainFilterEnum DomainFilter { get; set; }

        public Tab_Aut UserLevel
        {
            get
            {
                if (_userLevel == null)
                {
                    if (TabAuts != null)
                        _userLevel = TabAuts.FirstOrDefault(tb => tb.Utenti_Id == User.Utenti_Id && tb.Tab_Funz_Id == null);
                }
                return _userLevel;
            }
        }

        /// <summary>
        /// Aggiungiamo l'utente dalla sessione corrente inquanto ha effettuato il login
        /// </summary>
        public void SetUser(Utenti user)
        {
            User = user;
        }

        /// <summary>
        /// Rimuoviamo l'utente dalla sessione corrente inquanto è scaduta oppure ha effettuato signout
        /// </summary>
        public void RemoveUser()
        {
            _log.InfoFormat("Utente {0} ha effettuato il signout", User.Codice_Utente);

            User = null;
        }

        public static T GetFromSession<T>(String sessionKey)
        {
            if (HttpContext.Current.Session != null && HttpContext.Current.Session[sessionKey] != null)
                return (T)HttpContext.Current.Session[sessionKey];
            return default(T);
        }

        public static void SetToSession<T>(String sessionKey, T value)
        {
            if (HttpContext.Current.Session != null)
                HttpContext.Current.Session.Add(sessionKey, value);
        }

        public static void LogOut()
        {
            if (PowerWebContext.Current != null)
            {
                PowerWebContext.Current.RemoveUser();

                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session.Abandon();

                FormsAuthentication.SignOut();
            }

            HttpContext.Current.Response.Redirect(CommonService.BaseSiteUrl);
        }
    }
}
