using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using Data;
using DevExpress.XtraPrinting.Native;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Tab_OrariTipoRepository : GenericRepository<Tab_Orari_Tipo>, ITab_OrariTipoRepository
    {
        #region Constructors

        public Tab_OrariTipoRepository(PowerWebEntities context)
            : base(context)
        {
        }

        #endregion

        #region Private Methods

        private static void ResetSession()
        {
        }

        #endregion

        #region Public Methods

        public override Dictionary<string, string> Check(Tab_Orari_Tipo entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();

            try
            {
                // nella compilazione di un tab orari tipo è obbligatorio valorizzare l'entità di riferimento
                if (String.IsNullOrEmpty(entity.Tab_Orari_Tipo_Entita_Rif))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Entita_Rif),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TAB_ORARI_TIPO_ENTITA_RIF));

                // nella compilazione di un nuovo tab orari tipo non è possibile cambiare l'entità di riferimento se ci sono già delle altre entità
                // con collegato questo tipo orario
                if (!isNew)
                {
                    // in base all'entità di riferimento, verifico che sul repository dell'altra entità non ci siano record collegati
                    if (!String.IsNullOrEmpty(entity.Tab_Orari_Tipo_Entita_Rif))
                    {
                        bool hasOtherEntityRecors = false;

                        if (entity.Tab_Orari_Tipo_Entita_Rif == "Col") // se sto inserendo un valore collaboratore, controllo che non ci siano cantieri collegati
                        {
                            hasOtherEntityRecors = RepoManager.CantRepo.DbSet.Any(cant => cant.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id);
                        }
                        else if (entity.Tab_Orari_Tipo_Entita_Rif == "Can") // se sto inserendo un valore cantiere, controllo che non ci siano collaboratori collegati
                        {
                            hasOtherEntityRecors = RepoManager.ColRepo.DbSet.Any(col => col.Tab_Orari_Tipo_Id == entity.Tab_Orari_Tipo_Id);
                        }

                        if (hasOtherEntityRecors)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Entita_Rif),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_NON_POSSIBILE_MOD_ENTITA_CON_COLLEGAMENTI_PRESENTI));
                    }
                }

                //      
                //1) Si verifica che non ci sia due volte con la stessa descrizzione uno stesso orario tipo
                //
                if (String.IsNullOrEmpty(entity.Tab_Orari_Tipo_Desc))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Desc),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TAB_ORARI_TIPO_DESC));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_OrariTipoRepo.SingleOrDefault(u => u.Tab_Orari_Tipo_Desc== entity.Tab_Orari_Tipo_Desc) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Desc),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Tab_OrariTipoRepo.SingleOrDefault(u => u.Tab_Orari_Tipo_Desc == entity.Tab_Orari_Tipo_Desc && u.Tab_Orari_Tipo_Id != entity.Tab_Orari_Tipo_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Desc),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //  
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                
                // Verifico che se uno dei campi di definizione del notturno è valorizzato lo sia anche l'altro
                if (entity.Tab_Orari_Tipo_Inizio_Not != null || entity.Tab_Orari_Tipo_Fine_Not != null)
                {
                    if (entity.Tab_Orari_Tipo_Inizio_Not == null || entity.Tab_Orari_Tipo_Fine_Not == null)
                    {
                        string campoX = String.Empty;
                        string campoY = String.Empty;
                        if (entity.Tab_Orari_Tipo_Inizio_Not == null)
                        {
                            campoX = BusinessService.GetLocalizedString(PowerWebResources.FLD_TAB_ORARI_TIPO_INIZIO_NOT);
                            campoY = BusinessService.GetLocalizedString(PowerWebResources.FLD_TAB_ORARI_TIPO_FINE_NOT);
                        }
                        else
                        {
                            campoX = BusinessService.GetLocalizedString(PowerWebResources.FLD_TAB_ORARI_TIPO_FINE_NOT);
                            campoY = BusinessService.GetLocalizedString(PowerWebResources.FLD_TAB_ORARI_TIPO_INIZIO_NOT);
                        }

                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Orari_Tipo_Inizio_Not),
                              BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_VALORE_CAMPO_X_MANCANTE_SE_CAMPO_Y_PRESENTE, campoX, campoY));
                    }
                }

                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
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
                var CodErr = "Tab_Orari_Tipo_Id= " + entity.Tab_Orari_Tipo_Id;
                throw ex;
            }
            return result;
        }

        #endregion
    }
}