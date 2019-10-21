using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public partial class Utenti
    {
        public String Password
        {
            get;
            set;
        }

        public int N_Utenti_Fil
        {
            get
            {
                return Utenti_Fil.Count;
            }
        }

        public int N_Utenti_Resp
        {
            get
            {
                return Utenti_Resp.Count;
            }
        }

        public int Liv_Utente
        {
            get
            {
                int currentLevel = 0;
                var currentAuthLevel = this.Tab_Aut.SingleOrDefault(auth => auth.Tab_Funz_Id == null);
                if (currentAuthLevel != null)
                    currentLevel = currentAuthLevel.Funz_Aut;
                return currentLevel;
            }
            set
            {
                var currentAuthLevel = this.Tab_Aut.SingleOrDefault(auth => auth.Tab_Funz_Id == null);
                if (currentAuthLevel != null)
                    currentAuthLevel.Funz_Aut = value;
            }
        }

        private int _liv_Utente_Edit = 0;

        public int Liv_Utente_Edit
        {
            get
            {
                int currentLevel = _liv_Utente_Edit;
                var currentAuthLevel = this.Tab_Aut.SingleOrDefault(auth => auth.Tab_Funz_Id == null);
                if (currentAuthLevel != null)
                    currentLevel = _liv_Utente_Edit = currentAuthLevel.Funz_Aut;
                return currentLevel;
            }
            set
            {
                _liv_Utente_Edit = value;
            }
        }

        /// <summary>
        /// Recupera o imposta l'autorizzazione specifica dell'utente.
        /// </summary>
        /// <value>
        /// L'autorizzazione specifica dell'utente.
        /// </value>
        public Tab_Aut AutUtente
        {
            get
            {
                return Tab_Aut.SingleOrDefault(auth => auth.Tab_Funz_Id == null);
            }
        }

        /// <summary>
        /// Verifica se per l'operazione indicata e l'autorizzazione specifica l'utente è abilitato all'esecuzione.
        /// </summary>
        /// <param name="operationType">Il tipo di operazione da eseguire.</param>
        /// <param name="funcAuthorization">L'autorizzazione di riferimento per l'operazione.</param>
        /// <param name="defaultLevel">Il livello di default da applicare in caso di autorizzazione mancante</param>
        /// <returns><c>True</c> in caso l'utente risulti autorizzato ad eseguire l'operazione; altrimenti <c>false</c></returns>
        public bool IsUserAutorized(OperationTypeEnum operationType, Tab_Aut funcAuthorization, int defaultLevel)
        {
            // inizializzazione del valore di ritorno del metodo
            bool isAuthorized = false;
            
            // in base al tipo di operazione calcolo il livello di controllo dell'autorizzazione sulla tab_aut
            int funcLevel = defaultLevel;
            switch (operationType)
            {
                case OperationTypeEnum.FuncAccess:
                    funcLevel = funcAuthorization == null || AutUtente == null ? defaultLevel : funcAuthorization.Funz_Aut;
                    break;
                case OperationTypeEnum.Edit:
                    funcLevel = funcAuthorization == null || AutUtente == null ? defaultLevel : funcAuthorization.Mod_Aut;
                    break;
                case OperationTypeEnum.Insert:
                    funcLevel = funcAuthorization == null || AutUtente == null ? defaultLevel : funcAuthorization.Ins_Aut;
                    break;
                case OperationTypeEnum.Delete:
                    funcLevel = funcAuthorization == null || AutUtente == null ? defaultLevel : funcAuthorization.Del_Aut;
                    break;
            }
            
            // in base al tipo di operazione controllo uno specifico valore nelle autorizzazioni
            switch (operationType)
            {
                case OperationTypeEnum.FuncAccess: // autorizzazione alla funzione
                    isAuthorized = funcLevel <= AutUtente.Funz_Aut;
                    break;
                case OperationTypeEnum.Edit: // autorizzazione alla modifica dei dati della funzione
                    isAuthorized = funcLevel <= AutUtente.Mod_Aut;
                    break;
                case OperationTypeEnum.Insert: // autorizzazione all'inserimento dei dati della funzione
                    isAuthorized = funcLevel <= AutUtente.Ins_Aut;
                    break;
                case OperationTypeEnum.Delete: // autorizzazione alla cancellazione dei dati della funzione
                    isAuthorized = funcLevel <= AutUtente.Del_Aut;
                    break;
            }

            // ritorno del valore calcolato dal metodo
            return isAuthorized;
        }

        #region Class Enumns

        /// <summary>
        /// Il tipo di opeazione (utilizzata per il controllo delle autorizzazioni)
        /// </summary>
        public enum OperationTypeEnum
        {
            /// <summary>
            /// Accesso alla funzione
            /// </summary>
            FuncAccess,

            /// <summary>
            /// Modifica
            /// </summary>
            Edit,

            /// <summary>
            /// Inserimento
            /// </summary>
            Insert,

            /// <summary>
            /// Cancellazione
            /// </summary>
            Delete
        }

        #endregion
    }
}
