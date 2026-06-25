using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Business.Profile;
using Business.Repository;
using Domain;
using Newtonsoft.Json.Linq;

namespace PowerWeb.Api
{

    /// <summary>
    /// Api generica utilizzata per strutturare le api esposte di Power Web utilizzando il template pattern
    /// </summary>
    public abstract class GenericApi : ApiController
    {

        #region Private Fields

        /// <summary>
        /// La lista degli errori utilizzati dalle API
        /// </summary>
        private List<KeyValuePair<string, string>> _errors;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Recupera la lista degli errori utilizzati dalle API.
        /// </summary>
        /// <value>
        /// La lista degli errori utilizzati dalle API.
        /// </value>
        protected List<KeyValuePair<string, string>> Errors
        {
            get
            {
                return _errors ?? (_errors = new List<KeyValuePair<string, string>>());
            }
        }

        /// <summary>
        /// Recupera il json della risposta dell API.
        /// </summary>
        /// <value>
        /// Il json contenente i dati estrapolati.
        /// </value>
        public JObject Json
        {
            get
            {
                return Json;
            }
            set { 
            }
        }

        /// <summary>
        /// Recupera o imposta la data di inizio del periodo di riferimento per l'operazione.
        /// </summary>
        /// <value>
        /// La data di inizio del periodo di riferimento per l'operazione.
        /// </value>
        protected DateTime From { get; set; }

        /// <summary>
        /// Recupera o imposta la data di fine del periodo di riferimento per l'operazione.
        /// </summary>
        /// <value>
        /// La data di fine del periodo di riferimento per l'operazione.
        /// </value>
        protected DateTime To { get; set; }

        /// <summary>
        /// Recupera o imposta la stringa con i dati Json utilizzati dalla api in riferimento all'operazione da eseguire.
        /// </summary>
        /// <value>
        /// La stringa con i dati Json utilizzat dalla api in riferimento all'operazione da eseguire.
        /// </value>
        public string JsonData { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del collaboratore di cui cercare le timbrature.
        /// </summary>
        /// <value>
        /// Il valore utilizzato nella api di komplett.
        /// </value>
        public int ColId { get; set; }

        /// <summary>
        /// Recupera o imposta l'id del cantiere di cui cercare le timbrature.
        /// </summary>
        /// <value>
        /// Il valore utilizzato nella api di komplett.
        /// </value>
        public int CantId { get; set; }

        /// <summary>
        /// Recupera o imposta la stringa contenente la motivazione delle richieste.
        /// </summary>
        /// <value>
        /// La stringa con la motivazione.
        /// </value>
        public string Justification { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di inizio dei possibili permessi richiesti.
        /// </summary>
        /// <value>
        /// L'ora d'inizio.
        /// </value>
        protected TimeSpan Start { get; set; }

        /// <summary>
        /// Recupera o imposta l'ora di fine dei possibili permessi richiesti.
        /// </summary>
        /// <value>
        /// L'ora di fine.
        /// </value>
        protected TimeSpan End { get; set; }

        /// <summary>
        /// Recupera o imposta la stringa contenente la motivazione delle richieste.
        /// </summary>
        /// <value>
        /// La stringa con la motivazione.
        /// </value>
        public double Durata { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede i dati passati tramite una stringa contenente del json
        /// </summary>
        /// <param name="jsonData">i dati oggetto della API in formato json.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public IEnumerable<string> Get(string jsonData)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                JsonData = jsonData;

                // si procede all'elaborazione solamente se le date sono coerenti
                if (!String.IsNullOrEmpty(JsonData))
                    ExecuteOperation();
                else
                    Errors.Add(new KeyValuePair<string, string>("JsonData", String.Format("Stringa Json non valorizzata (JsonData: {0})", JsonData)));
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseErrorForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede un from e un to.
        /// </summary>
        /// <param name="from">La data di inizio elaborazione.</param>
        /// <param name="to">La data di fine elaborazione.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public string Get(DateTime from, DateTime to)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                From = from;
                To = to;

                // si procede all'elaborazione solamente se le date sono coerenti
                if (from <= to)
                    ExecuteOperation();
                else
                    Errors.Add(new KeyValuePair<string, string>("Date", String.Format("Date passate come parametro non coerenti (from: {0}; to: {1})", from, to)));
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseJsonrForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede un from e un to.
        /// </summary>
        /// <param name="from">La data di inizio elaborazione.</param>
        /// <param name="to">La data di fine elaborazione.</param>
        /// <param name="matricola">La matricola del collaboratore.</param>
        /// <param name="justification">La motivazione inserita.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public string Get(DateTime from, DateTime to, int matricola, string justification)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                From = from;
                To = to;
                ColId = matricola;
                Justification = justification;
                Start = new TimeSpan(0,1,0);

                // si procede all'elaborazione solamente se le date sono coerenti
                if (from <= to)
                    ExecuteOperation();
                else
                    Errors.Add(new KeyValuePair<string, string>("Date", String.Format("Date passate come parametro non coerenti (from: {0}; to: {1})", from, to)));
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseJsonrForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede un from e un to.
        /// </summary>
        /// <param name="from">La data di inizio elaborazione.</param>
        /// <param name="start">La data di inizio elaborazione.</param>
        /// <param name="end">La data di fine elaborazione.</param>
        /// <param name="matricola">La matricola del collaboratore.</param>
        /// <param name="justification">La motivazione inserita.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public string Get(DateTime from, int matricola, string justification, TimeSpan start, TimeSpan end)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                From = from;
                ColId = matricola;
                Justification = justification;
                Start = start;
                End = end;
                ExecuteOperation();
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseJsonrForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede un from e un to.
        /// </summary>
        /// <param name="from">La data di inizio elaborazione.</param>
        /// <param name="start">La data di inizio elaborazione.</param>
        /// <param name="end">La data di fine elaborazione.</param>
        /// <param name="matricola">La matricola del collaboratore.</param>
        /// <param name="justification">La motivazione inserita.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public string Get(DateTime from, int matricola, string justification, double durata)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                From = from;
                ColId = matricola;
                Justification = justification;
                Durata = durata;
                Start = new TimeSpan(0, 1, 0);
                ExecuteOperation();
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseJsonrForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// L'operazione per questo overload di get richiede un from e un to.
        /// </summary>
        /// <param name="from">La data di inizio elaborazione.</param>
        /// <param name="to">La data di fine elaborazione.</param>
        /// <returns>L'elenco degli errori riscontrati durante l'esecuzione dell'operazione.</returns>
        public string Get(DateTime date, int colId, int cantId)
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
            {
                // inserimento dei parametri nell'oggetto
                From = date;
                ColId = colId;
                CantId = cantId;

                // si procede all'elaborazione solamente se le date sono coerenti
                ExecuteOperation();
            }
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseJsonrForReturnValue();
        }

        /// <summary>
        /// Funzione di get della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// </summary>
        /// <returns>L'elenco degli errori di elaborazione eventualmente riscontrati</returns>
        public IEnumerable<string> Get()
        {
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
                ExecuteOperation();
            else
                AddUserError();

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return ParseErrorForReturnValue();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione richiesta dalla API.
        /// </summary>
        protected abstract void ExecuteOperation();

        #endregion

        #region Private Methods

        /// <summary>
        /// Converte l'elenco degli errori di procedura nel formato di ritorno della API.
        /// </summary>
        /// <returns>L'elenco degli errori in formato ritornabile dalla API</returns>
        private IEnumerable<string> ParseErrorForReturnValue()
        {
            return Errors.Any() ? Errors.Select(kvp => kvp.Value).AsEnumerable() : new List<string>() { String.Empty };
        }

        /// <summary>
        /// Converte l'elenco degli errori di procedura nel formato di ritorno della API.
        /// </summary>
        /// <returns>L'elenco degli errori in formato ritornabile dalla API</returns>
        private string ParseJsonrForReturnValue()
        {
            return JsonData;//Json.ToString();
        }

        /// <summary>
        /// Aggiunge all'elenco degli errori il messaggio di mancato accesso alla procedura per utente.
        /// </summary>
        private void AddUserError()
        {
            Errors.Add(new KeyValuePair<string, string>("Avvio", "Utente di gestione API non configurato a sistema!"));
        }

        /// <summary>
        /// Inizializza l'utente api utilizzato per lavorare in sessione sull'ambiente di Power Web.
        /// </summary>
        /// <returns><c>true</c> in caso di corretta effettuazione dell'autenticazione; altrimenti<c>false</c></returns>
        private bool InitializeApiUser()
        {
            // inizializzazione del valore di ritorno del metodo
            bool userInitialized = false;

            // calcolo dell'utente con cui eseguire le operazioni
            Utenti apiUser = RepoManager.UtentiRepo.FirstOrDefault(ut => ut.Codice_Utente == Common.Properties.Settings.Default.APIUserName);

            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (apiUser != default(Utenti))
            {
                PowerWebMembershipProvider.InitializeUser(apiUser);
                userInitialized = true;
            }

            // se l'inizializzazione è andata a buon fine allora verifico che anche
            // il modulo della schedulazione sia abilitato
            if (userInitialized)
                userInitialized = RepoManager.ParamRepo.ParametersRow.Abilita_Schedulatore;

            // ritorno del valore calcolato dal metodo
            return userInitialized;
        }

        #endregion

    }
}