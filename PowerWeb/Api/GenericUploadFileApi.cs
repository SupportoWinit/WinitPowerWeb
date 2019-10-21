using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Business.Profile;
using Business.Repository;
using Domain;

namespace PowerWeb.Api
{
    /// <summary>
    /// Api generica utilizzata per strutturare le api di upload dei file esposte di Power Web utilizzando il template pattern
    /// </summary>
    public abstract class GenericUploadFileApi : ApiController
    {

        #region Public Methods

        /// <summary>
        /// Funzione di post della api corrente; utilizza il template pattern per l'operazione da eseguire.
        /// </summary>
        /// <returns>L'elenco degli errori di elaborazione eventualmente riscontrati</returns>
        public HttpResponseMessage Post()
        {
            // inizializzazione della response del metodo
            var response = new HttpResponseMessage();

           
            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
                response.StatusCode = ExecuteOperation();
            else
                response.StatusCode = HttpStatusCode.Unauthorized;


            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return response;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Esegue l'operazione richiesta dalla API.
        /// </summary>
        /// <returns>La risposta da ritornare all'invocatore della API</returns>
        protected abstract HttpStatusCode ExecuteOperation();

        #endregion

        #region Private Methods

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