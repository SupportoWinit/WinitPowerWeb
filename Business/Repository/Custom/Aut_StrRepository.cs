using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Data;
using Domain;

namespace Business.Repository.Custom
{
    /// <summary>
    /// Classe repository utilizzata per esporre le funzionalità riguardanti l'autorizzazione degli straordinari
    /// </summary>
    public class Aut_StrRepository : GenericRepository<Aut_Str>, IAut_StrRepository
    {

        #region Constructors

        /// <summary>
        /// Inizializza la nuova istanza di una classe di tipo <see cref="Aut_StrRepository"/>.
        /// </summary>
        /// <param name="context">Il database utilizzato dal repository.</param>
        public Aut_StrRepository(PowerWebEntities context) : base(context) { }

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Verifica la coerenza dei dati della specifica entità.
        /// </summary>
        /// <param name="entity">L'entità dei cui dati controllare la coerenza.</param>
        /// <param name="isNew"><c>true</c>. se è richiesto di processare un nuovo record; altrimenti <c>false</c></param>
        /// <param name="isResetSession"><c>true</c> se è richiesto un reset di sessione prima di effettuare la check; altrimenti <c>false</c>.</param>
        /// <returns>Un dizionario contenente gli eventuali errori di coerenza riscontrati dal metodo sulla specifica entità.</returns>
        public override Dictionary<string, string> Check(Aut_Str entity, bool isNew = false, bool isResetSession = true)
        {
            // inizializzazione del valore di ritorno del metodo
            var errors = new Dictionary<string, string>();
            
            // per prima cosa si aggiungono gli errori di eventuali controlli generali
            errors.AddRange(base.Check(entity, isNew, isResetSession));

            // se non ci sono stati errori nei controlli generali
            if (!errors.Any())
            {
                // VERIFICA DELL'OBBLIGATORIETA' DEI CAMPI

                // il collabortore deve necessariamente essere collegato
                if (entity.Aut_Str_Col_Id == 0)
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Col_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_AUT_STR_COL_ID));

                // la data di inizio e la data di fine sono obbligatorie
                if (entity.Aut_Str_Data_Inizio == default(DateTime))
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Data_Inizio),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_AUT_STR_DATA_INIZIO));
                if (entity.Aut_Str_Data_Fine == default(DateTime))
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Data_Fine),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_AUT_STR_DATA_FINE));

                // il numero di ore autorizzate è obblibatorio
                if (entity.Aut_Str_Num_Ore == default(int))
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Num_Ore),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_AUT_STR_NUM_ORE));

                // CONTROLLO COERENZA DATI

                // la data di fine non deve superare la data di inizio
                if (entity.Aut_Str_Data_Inizio > entity.Aut_Str_Data_Fine)
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Data_Inizio),
                        BusinessService.GetLocalizedString(PowerWebResources.STR_PERIODO_NON_CORRETTO));

                // CONTROLLO COERENZA ALTRE ENTITA'

                // un'autorizzazione stroardinari per un collaboratore non può essere sovrapposta ad altre autorizzazioni per lo stesso collaboratore
                IEnumerable<Aut_Str> otherAutStr = Find(autStr => autStr.Aut_Str_Col_Id == entity.Aut_Str_Col_Id && autStr.Aut_Str_Id != entity.Aut_Str_Id &&
                                                                  ((entity.Aut_Str_Data_Fine >= autStr.Aut_Str_Data_Inizio && autStr.Aut_Str_Data_Fine >= entity.Aut_Str_Data_Fine)
                                                                  || (entity.Aut_Str_Data_Inizio >= autStr.Aut_Str_Data_Inizio && entity.Aut_Str_Data_Inizio <= autStr.Aut_Str_Data_Fine)));

                if (otherAutStr.Any())
                    errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Data_Inizio),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_SOVRAPPOSIZIONE));

                // non è possibile modificare/aggiungere elementi che comportano la modifica di elementi antecedenti alla data blocco
                if (RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.HasValue)
                    if (entity.Aut_Str_Data_Inizio <= RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value)
                        errors.AddOrAppend(CommonService.GetPropertyName(() => entity.Aut_Str_Data_Inizio),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_X_MINORE_DI_DATA_BLOCCO, PowerWebResources.FLD_AUT_STR_DATA_INIZIO));
            }

            // ritorno degli errori calcolati dal metodo
            return errors;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determina se lo specifico collaboratore nella specifica data risulta autorizzato agli straordinari
        /// e per quante ore.
        /// </summary>
        /// <param name="colId">L'identificativo del collaboratore di cui verificare l'autorizzazione.</param>
        /// <param name="date">La data in cui verificare l'autorizzazione.</param>
        /// <returns>
        /// Il numero di ore in cui risulta autorizzato il collaboratore; 0 se non autorizzato.
        /// </returns>
        public int AutStrColAuthorization(int colId, DateTime date)
        {
            // di default non sono autorizzate ore di straordinario
            int authorizedHours = 0;

            // viene verificata la presenza di un record di autorizzazione straordinario per il collaboratore indicato
            // nella data indicata; se presente si ritorna il numero di ore in esso configurato, altrimenti si ritorna
            // il valore di default precedentemente impostato
            Aut_Str colAutStr = FirstOrDefault(autStr => autStr.Aut_Str_Col_Id == colId && date >= autStr.Aut_Str_Data_Inizio && date <= autStr.Aut_Str_Data_Fine);
            if (colAutStr != default(Aut_Str))
                authorizedHours = colAutStr.Aut_Str_Num_Ore;

            // ritorno del numero di ore autorizzate per il collaboratore specificato
            // nella data specificata
            return authorizedHours;
        }

        #endregion

    }
}
