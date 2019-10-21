using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Business.Profile;
using Business.Repository;
using Common;
using DevExpress.Internal;
using Domain;

namespace PowerWeb.Api
{
    // TODO: valutare riutilizzazione del codice nella api (parametri e ritorno generico)
    /// <summary>
    /// Classe utilizzata per esporre tramite web api il recupero della descrizione di una specifica associazione
    /// </summary>
    public class GetRegistryDescriptionController : ApiController
    {

        #region Public Methods

        /// <summary>
        /// Recupera la descrizione anagrafica per i dati di associazione specificati.
        /// </summary>
        /// <param name="deviceCode">Il codice badge/dispositivo di cui ricercare l'associazione.</param>
        /// <param name="associationDate">La data in cui ricercare l'associazione per il badge/dispositivo.</param>
        /// <returns>La stringa contenente la descrizione dell'anagrafica relativa ai dati di associazione specificati</returns>
        public string Get(string deviceCode, DateTime associationDate)
        {
            // inizializzazione del valore di ritorno del metodo
            string registryDescription = String.Empty;

            // si procede con l'elaborazione solamente se l'utente è stato trovato
            if (InitializeApiUser())
                registryDescription = GetDescription(deviceCode, associationDate);

            // ritorno degli errori eventualmente recuperati nell'elaborazione
            return registryDescription;
        }

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

            // ritorno del valore calcolato dal metodo
            return userInitialized;
        }

        /// <summary>
        /// Esegue la ricerca dell'anagrafica per la device e ne ritorna la descrizione (se trovata).
        /// </summary>
        /// <param name="deviceCode">Il codice della device da ricercare.</param>
        /// <param name="associationDate">La data di associazione da ricercare.</param>
        /// <returns>La stringa contenente la descrizione dell'anagrafica relativa all'associazione ricercata.</returns>
        private string GetDescription(string deviceCode, DateTime associationDate)
        {
            // inizializzazione del valore di ritorno del metodo
            string registryDescription = String.Empty;

            // recupero dell'oggetto contenente l'anagrafica di riferimento
            object pruFruRegistry = RepoManager.RegRepo.GetPruFruRegistry(CommonService.AggiungiSpaziASinistraSeStringaNumerica(deviceCode, 10), null);

            // si procede se il valore è presente in almeno un'anagrafica
            if (pruFruRegistry != null)
                // se si sta processando un'unità portatile
                if (pruFruRegistry is Pru)
                {
                    // è recuperata l'associazione per il codice indicato andando a pescare la prima disponibile per la data richiesta
                    Pru currentPru = pruFruRegistry as Pru;
                    Pru_Col pruColRegistry = RepoManager.Pru_ColRepo.Find(pruCol => pruCol.Pru_Id == currentPru.Pru_Id && pruCol.Abilitazione_Data_Inizio_Pru_Col <= associationDate && !pruCol.DisAbilitazione_Pru_Col, true)
                        .OrderByDescending(pru => pru.Abilitazione_Data_Inizio_Pru_Col).FirstOrDefault();

                    // se è stata trovata l'anagrafica si ritorna la descrizione del collaboratore collegato
                    if (pruColRegistry != default(Pru_Col))
                        registryDescription = pruColRegistry.CognomeNome_Col;
                }
                else // altrimenti, se si sta processando una fru
                {
                    // è recuperata l'associazione per il codice indicato andando a pescare la prima disponibile per la data richiesta
                    Fru currentFru = pruFruRegistry as Fru;
                    Fru_Cant fruCantRegistry = RepoManager.Fru_CantRepo.Find(fruCant => fruCant.Fru_Id == currentFru.Fru_Id && fruCant.Abilitazione_Data_Inizio_Fru_Can <= associationDate && !fruCant.DisAbilitazione_Fru_Can, true)
                        .OrderByDescending(fru => fru.Abilitazione_Data_Inizio_Fru_Can).FirstOrDefault();

                    // se è stata trovata l'anagrafica si ritorna la descrizione del collaboratore collegato
                    if (fruCantRegistry != default(Fru_Cant))
                        registryDescription = fruCantRegistry.Descrizione_Can;
                }

            // ritorno del valore calcolato dal metodo
            return registryDescription;
        }

        #endregion

    }
}