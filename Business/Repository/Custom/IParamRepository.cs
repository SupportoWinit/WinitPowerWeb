using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Domain;
using Common;

namespace Business.Repository.Custom
{
    public interface IParamRepository : IRepository<Param>
    {
        Param ParametersRow { get; }

        void ResetParametersRow();

        /// <summary>
        /// Metodo che dato un enum ritorna il valore corrispondente configurato
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <param name="isBackground">Se si sta cercando un valore di background è <c>true</c>. Altrimenti <c>false</c></param>
        /// <returns>Il colore configurato rispetto all'enum passato come parametro</returns>
        Color GetColorFromEnum(Enum enumToSearch, bool isBackgruound);

        /// <summary>
        /// Dall'xml delle configurazioni per l'import/export dati verso l'esterno recupero
        /// l'host in base al tipo di classe che lo richiede
        /// </summary>
        /// <param name="fullName">Il tipo della classe</param>
        /// <returns></returns>
        string GetConnectionHost(string fullName);
        string GetUser(string fullName);
        string GetPassword(string fullName);
        string GetTenant(string fullName);

        /// <summary>
        /// Dall'xml delle configurazioni per l'import/export dati verso l'esterno recupero
        /// il path per le api di update cantiere in base al tipo di classe che lo richiede
        /// </summary>
        /// <param name="fullName">Il tipo della classe</param>
        /// <returns></returns>
        string GetCantInsertApi(string fullName);
        /// <summary>
        /// Dall'xml delle configurazioni per l'import/export dati verso l'esterno recupero
        /// il path per le api di update cantiere in base al tipo di classe che lo richiede
        /// </summary>
        /// <param name="fullName">Il tipo della classe</param>
        /// <returns></returns>
        string GetCantUpdateApi(string fullName);
        /// <summary>
        /// Dall'xml delle configurazioni per l'import/export dati verso l'esterno recupero
        /// il path per le api di cancellazione cantiere in base al tipo di classe che lo richiede
        /// </summary>
        /// <param name="fullName">Il tipo della classe</param>
        /// <returns></returns>
        string GetCantDeleteApi(string fullName);

        /// <summary>
        /// Metodo che dato un enum ritorna il valore della corrispondente pesonalizzazione attivata
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <returns>Il valore della personalizzazione configurata. 0 equivale a pesonalizzazione non attiva</returns>
        int GetCustomizationFromEnum(Enum enumToSearch);

        /// <summary>
        /// Metodo che dato un enum e il nomne di un parametro ritorna il valore del parametro per la personalizzazione attivata
        /// </summary>
        /// <param name="enumToSearch">l'enum da ricercare</param>
        /// <param name="paramNameToSearch">Il nome del parametro da ricercare (case sensitive)</param>
        /// <returns>Il valore assunto dal parametro per la personalizzazione (String.Empty se non presente o se personalizzazione non attiva).</returns>
        string GetCustomizationParamFromEnum(Enum enumToSearch, string paramNameToSearch);

        /// <summary>
        /// Determina se è in corso un inmport/elaborazione
        /// </summary>
        bool IsElaborationReady();

        /// <summary>
        /// Setta il flag di elaborazione avviata a true
        /// </summary>
        bool LockElaboration();

        /// <summary>
        /// Setta il flag di elaborazione avviata a false
        /// </summary>
        void UnLockElaboration();

        bool IsCurrentUserCustomizationEnabled(CustomizationEnum customization, string parameterName);

        /// <summary>
        /// Salva l'ultimo indice per lo scarico aggiornato delle timbrature dal server geobadge
        /// </summary>
        /// <param name="index">L'indice corrente</param>
        void SaveGeoBadgeRegIndex(int index);

        /// <summary>
        /// Recupera la configurazione generale del notturno presente nella scheda parametri.
        /// Composta da:
        /// - Variabile booleana che indica se il modulo del notturno risulta attivo.
        /// - Il tipo di notturno configurato.
        /// - La nuova mezzanotte configurata.
        /// </summary>
        /// <value>
        /// La configurazione generale del notturno presente nella scheda parametri.
        /// Composta da:
        /// - Variabile booleana che indica se il modulo del notturno risulta attivo.
        /// - Il tipo di notturno configurato.
        /// - La nuova mezzanotte configurata.
        /// </value>
        Tuple<bool, NocturneTypeEnum, TimeSpan,TimeSpan> NocturneGeneralConfiguration { get; }

    }
}
