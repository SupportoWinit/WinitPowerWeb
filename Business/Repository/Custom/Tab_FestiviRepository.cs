using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Tab_FestiviRepository : GenericRepository<Tab_Festivi>, ITab_FestiviRepository
    {
        public Tab_FestiviRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static void ResetSession()
        {
        }

        public override Dictionary<string, string> Check(Tab_Festivi entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();
            try
            {
                //  
                //NON ESEGUO il Controllo sulla DataOraUltimaModifica perchè questa tabella NON ha questa informazione
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //     
                if (CommonService.Nz(entity.Giorno_Tab_Festivi, new DateTime(1, 1, 1)) == new DateTime(1, 1, 1))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Giorno_Tab_Festivi),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_GIORNO_TAB_FESTIVI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_FestiviRepo.SingleOrDefault(u => u.Giorno_Tab_Festivi == entity.Giorno_Tab_Festivi) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Giorno_Tab_Festivi),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Tab_FestiviRepo.SingleOrDefault(u => u.Giorno_Tab_Festivi == entity.Giorno_Tab_Festivi &&
                          u.Tab_Festivi_Id != entity.Tab_Festivi_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Festivi_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //      
                if (CommonService.Nz(entity.Descrizione_Tab_Festivi, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Tab_Festivi),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_TAB_FESTIVI));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                // NESSUN CONTROLLO DI QUESTO TIPO
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Descrizione_Tab_Festivi, "") != "")
                    if (entity.Descrizione_Tab_Festivi.Length > 100)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Tab_Festivi),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DESCRIZIONE_TAB_FESTIVI, PowerWebResources.VALORE_100));
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
                var CodErr = "Tab_Festivi_Id: " + entity.Tab_Festivi_Id;
                throw ex;
            }
            return result;
        }


        public bool IsHolidayOrNotWorkDays(DateTime date)
        {
            return FirstOrDefault(day => day.Giorno_Tab_Festivi == date.Date || (date.DayOfWeek == DayOfWeek.Saturday && !RepoManager.ParamRepo.ParametersRow.Sabato_Feriale) || date.DayOfWeek == DayOfWeek.Sunday, true) != null;
        }

        public DateTime NextValidDay(DateTime date)
        {
            while (IsHolidayOrNotWorkDays(date))
                date = date.AddDays(1);

            return date;
        }

    }
}
