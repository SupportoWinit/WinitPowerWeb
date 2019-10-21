using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using System.Text;
using Common;
using Business.MDBSchema;
using System.Data;
using System.Linq.Expressions;
using log4net;

namespace Business.Repository.Custom
{
    class Menu_TipoRepository : GenericRepository<Menu_Tipo>, IMenu_TipoRepository
    {
        public Menu_TipoRepository(PowerWebEntities context)
            : base(context)
        {
        }
       
        private static void ResetSession()
        {
        }

        public override Menu_Tipo Init()
        {
            Menu_Tipo oNewRecord = base.Init();
            //oNewRecord.DisAbilitazione_Fil = false;
            //oNewRecord.Data_Registrazione_Fil = DateTime.UtcNow;
            //oNewRecord.DataOraUltimaModifica_Fil = DateTime.UtcNow;
            return oNewRecord;
        }

        public override Dictionary<string, string> Check(Menu_Tipo entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();

            try
            {                
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Nome_Risorsa_Menu_Tipo))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa_Menu_Tipo ),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_RISORSA_TIPO_MENU ));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Menu_TipoRepo.SingleOrDefault(u => u.Nome_Risorsa_Menu_Tipo == entity.Nome_Risorsa_Menu_Tipo) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa_Menu_Tipo),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Menu_TipoRepo.SingleOrDefault(u => u.Nome_Risorsa_Menu_Tipo == entity.Nome_Risorsa_Menu_Tipo && u.Menu_Tipo_Id != entity.Menu_Tipo_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa_Menu_Tipo),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //                  
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Nome_Risorsa_Menu_Tipo, "") != "")
                    if (entity.Nome_Risorsa_Menu_Tipo.Length > 100)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa_Menu_Tipo),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_RISORSA_TIPO_MENU, PowerWebResources.VALORE_100 ));                
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
                var CodErr = "menu_Tipo_Id= " + entity.Menu                                                                                ;
                throw ex;
            }
            return result;
        }
    }
}
