using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Collections;
using System.Text;
using System.Linq.Dynamic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using Business.DataClasses.DevExtremeUtilities;
using DevExtreme.AspNet.Data;

namespace Business.Repository.Custom
{
    #region ENUM
    public enum TabGridLookupSearchFilterEnum
    {
        None,
        StartsWith,
        Contains,
    }

    public enum TabGridLookupSearchNameEnum
    {
        Cantiere_Viaggio,
        Cod_Funz_Aut,
        Codice_Cantiere_Col,
        Codice_Cantiere_Col_Orario,
        Codice_Cantiere_Fru_Can,
        Codice_Cantiere_Reg,
        Codice_Cantiere_Tab_Orari,
        Codice_Cliente_Can,
        Codice_Collaboratore_Col_Orario,
        Codice_Collaboratore_Pru_Col,
        Codice_Collaboratore_Reg,
        Codice_Fru_Can,
        Codice_Fru_Reg,
        Codice_Pru_Col,
        Codice_Pru_Reg,
        Codice_Resp_Can,
        Codice_Utente,
        Codice_Utente_Aut,
        Cognome_Col,
        Descrizione_Can,
        Domicilio_Luogo_Cli,
        Domicilio_Luogo_Col,
        Inserita_Da_Can_Note,
        Inserita_Da_Col_Note,
        Luogo_Can,
        Luogo_Can_,
        N_Serie_Fru,
        N_Serie_Pru,
        Luogo_Nascita_Can,
        Nascita_Luogo_Col,
        Nome_Col,
        NomeMenu_Utente,
        Residenza_Luogo_Cli,
        Residenza_Luogo_Col,
        Tipo_Orario_Col_Orario,
    }
    #endregion

    public class Tab_GridLookupRepository : GenericRepository<Tab_GridLookup>, ITab_GridLookupRepository
    {
        const string TAB_PROV = "TAB_PROV";
        const string TAB_COMUNI = "TAB_COMUNI";
        const string TAB_DECOD = "TAB_DECOD";
        const string CANT = "CANT";
        const string COL = "COL";
        const string RESP = "RESP";
        const string FIL = "FIL";
        const string TAB_FUNZ = "TAB_FUNZ";

        Dictionary<String, IList> TabGridLookupValuesDictionary
        {
            get
            {
                var tabGridLookupValuesDictionary = PowerWebContext.GetFromSession<Dictionary<String, IList>>("General_TabGridLookupValuesDictionary");
                if (tabGridLookupValuesDictionary == null)
                {
                    tabGridLookupValuesDictionary = new Dictionary<string, IList>();
                    PowerWebContext.SetToSession<Dictionary<String, IList>>("General_TabGridLookupValuesDictionary", tabGridLookupValuesDictionary);
                }

                return tabGridLookupValuesDictionary;
            }
        }

        public Tab_GridLookupRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static void ResetSession()
        {
        }

        public override Tab_GridLookup Init()
        {
            Tab_GridLookup oNewRecord = base.Init();
            oNewRecord.Campo_Db = false;
            oNewRecord.Campo_Filtro = 0;
            oNewRecord.Campo_Localizzato = false;
            oNewRecord.Campo_NotOnlyInList = false;
            oNewRecord.Campo_Selezionato = false;
            oNewRecord.Campo_Video = false;
            return oNewRecord;
        }

        /// <summary>
        /// Searches the by field and value.
        /// </summary>
        /// <param name="tabGridLookups">The tab grid lookups.</param>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <param name="beginIndex">Index of the begin.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="dataSource">The data source.</param>
        /// <param name="isForReport">if set to <c>true</c> [is for report].</param>
        /// <returns></returns>
        public IList SearchByFieldAndValue(List<Tab_GridLookup> tabGridLookups, String field, String value, int? beginIndex = null, int? endIndex = null, IQueryable dataSource = null, bool isForReport = false)
        {
            //Ilist=Rappresenta una raccolta non generica di oggetti cui è possibile accedere singolarmente.
            //rappresenta la lista di oggetti che si vogliono mostrare nella combo
            IList queryList = null;

            //vengono estratti i cambi della GridLookup per quel nome di ricerca
            var tgls = tabGridLookups.Where(tgl => tgl.NomeRicerca == field).OrderBy(tgl => tgl.Ordinamento).ToList();

            if (tgls.Count() > 0)
            {
                //viene estratto il primo 
                var mainTGL = tgls.First();

                //se il NomeTab del campo da cercareè fra quelli sotto  
                if ((mainTGL.NomeTab.ToUpper() == TAB_COMUNI ||
                     mainTGL.NomeTab.ToUpper() == TAB_DECOD ||
                     mainTGL.NomeTab.ToUpper() == TAB_PROV ||
                     mainTGL.NomeTab.ToUpper() == TAB_FUNZ) &&
                     String.IsNullOrEmpty(value) && endIndex.HasValue &&
                     endIndex.Value < RepoManager.ParamRepo.ParametersRow.ComboboxRowsPerPage &&
                     TabGridLookupValuesDictionary.ContainsKey(mainTGL.NomeRicerca))

                    //viene fatta la query
                    queryList = TabGridLookupValuesDictionary[mainTGL.NomeRicerca];

                else
                {
                    //viene estratto il nome del campo
                    String keyField = mainTGL.NomeCampo;

                    //viene caricato il dataset
                    var dbSet = dataSource;

                    //se il datasource è nullo
                    if (dbSet == null)
                    {
                        //viene estratto da quale tabella estrarre i dati
                        var currentType = Type.GetType(String.Format("Domain.{0}, Domain", mainTGL.NomeTab));
                        //vengono estratti i tutti i valori della tabella 
                        dbSet = RepoManager.Tab_DecodRepo.Context.Set(currentType);

                    }

                    //se si ha un db set valido
                    if (dbSet != null)
                    {
                        //stringa che rappresenta il where nella query
                        string whereQueryString = String.Empty;

                        if (isForReport)
                            whereQueryString = GetSQLWhereQuery(tgls, value, beginIndex, endIndex, campoDbReport: SingleOrDefault(tg => tg.NomeRicerca == field && tg.Campo_Db).NomeCampo);
                        else
                            //viene generato il where della query
                            whereQueryString = GetSQLWhereQuery(tgls, value, beginIndex, endIndex);

                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DoNotShowDisabledColCantId) == (int)DoNotShowDisabledColCantId.DoNotShow)
                        {
                            //se si stà lavorando sulla tabella dei collaboratori
                            if (mainTGL.NomeTab.ToUpper() == COL)
                            {
                                //se la stinga che effettua il where non è vuota allora  si aggiunge la condizione in AND
                                if (whereQueryString != String.Empty)
                                    whereQueryString = String.Format("{0} AND DisAbilitazione_Col=False", whereQueryString);

                                //se la stringa di where è vuoota si aggiunge direttamende la condizione di disabilitazione
                                else
                                    whereQueryString = String.Format("DisAbilitazione_Col=False");

                            }

                            //se si stà lavorando sulla tabella dei cantieri
                            if (mainTGL.NomeTab.ToUpper() == CANT)
                            {
                                //se la stinga che effettua il where non è vuota allora  si aggiunge la condizione in AND
                                if (whereQueryString != String.Empty)
                                    whereQueryString = String.Format("{0} AND DisAbilitazione_Can=False", whereQueryString);

                                //se la stringa di where è vuoota si aggiunge direttamende la condizione di disabilitazione
                                else
                                    whereQueryString = String.Format("DisAbilitazione_Can=False");

                            }
                        }


                        //viene prparata la query da eseguire successivamente
                        IQueryable query = dbSet;

                        //se sono attivi i filtri di figliale responsabile
                        if (PowerWebContext.Current.DomainFilter != DomainFilterEnum.None && dataSource == null)
                        {
                            if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil)
                            {
                                if (mainTGL.NomeTab.ToUpper() == FIL)
                                    query = RepoManager.Tab_DecodRepo.Context.Set<Fil>().Where(RepoManager.FilRepo.Filter);
                                else if (mainTGL.NomeTab.ToUpper() == CANT)
                                    query = RepoManager.Tab_DecodRepo.Context.Set<Cant>().Where(RepoManager.CantRepo.Filter);
                            }

                            if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp)
                            {
                                if (mainTGL.NomeTab.ToUpper() == RESP)
                                    query = RepoManager.Tab_DecodRepo.Context.Set<Resp>().Where(RepoManager.RespRepo.Filter);
                                else if (mainTGL.NomeTab.ToUpper() == COL)
                                    query = RepoManager.Tab_DecodRepo.Context.Set<Col>().Where(RepoManager.ColRepo.Filter);
                            }
                        }

                        //se è valorizzato il where della query
                        if (!String.IsNullOrEmpty(whereQueryString))
                            query = query.Where(whereQueryString);

                        query = query.Select(GetSQLSelectQuery(tgls, value));
                        query = query.OrderBy(keyField);

                        if (beginIndex.HasValue && endIndex.HasValue)
                            query = query.Skip(beginIndex.Value).Take(endIndex.Value - beginIndex.Value + 1);

                        //viene eseguita la query e il risulatao viene 
                        queryList = query.ToNonGenericList();

                        var toLocalizeFields = tgls.Where(tgl => tgl.Campo_Localizzato);

                        Type listType = null;

                        foreach (var toLocalizeField in toLocalizeFields)
                        {
                            PropertyInfo currentFieldPI = null;

                            foreach (var item in queryList)
                            {
                                if (listType == null)
                                    listType = item.GetType();

                                if (currentFieldPI == null)
                                    currentFieldPI = listType.GetProperty(toLocalizeField.NomeCampo);

                                var currentValue = currentFieldPI.GetValue(item, null);
                                if (currentValue != null)
                                {
                                    var currentpropValue = BusinessService.GetLocalizedString(currentValue.ToString());
                                    if (!String.IsNullOrEmpty(currentpropValue))
                                        currentFieldPI.SetValue(item, currentpropValue, null);
                                }
                            }

                            currentFieldPI = null;
                        }

                        if ((mainTGL.NomeTab.ToUpper() == TAB_COMUNI ||
                             mainTGL.NomeTab.ToUpper() == TAB_DECOD ||
                             mainTGL.NomeTab.ToUpper() == TAB_PROV ||
                             mainTGL.NomeTab.ToUpper() == TAB_FUNZ) &&
                             String.IsNullOrEmpty(value) && endIndex.HasValue &&
                             endIndex.Value < RepoManager.ParamRepo.ParametersRow.ComboboxRowsPerPage)
                            TabGridLookupValuesDictionary[mainTGL.NomeRicerca] = queryList;
                    }
                }
            }

            return queryList;
        }

        /// <summary>
        /// Ritorna il select della query
        /// </summary>
        /// <param name="tabGridLookups">The tab grid lookups.</param>
        /// <param name="value">The value.</param>
        /// <param name="beginIndex">Index of the begin.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns></returns>
        private static string GetSQLSelectQuery(List<Tab_GridLookup> tabGridLookups, String value, int beginIndex = -1, int endIndex = -1)
        {
            StringBuilder resultSB = new StringBuilder("new (");

            for (int i = 0; i < tabGridLookups.Count(); i++)
            {
                Tab_GridLookup tabGridLookup = tabGridLookups[i];

                resultSB.Append(tabGridLookup.NomeCampo);
                if (i + 1 < tabGridLookups.Count())
                    resultSB.Append(", ");
            }

            //viene generata una SELCT sui campipresenti nella tab grid lookup per il nome campo corrente
            return resultSB.Append(")").ToString();
        }

        /// <summary>
        /// Ritorna il where per la query di estrazione dati 
        /// </summary>
        /// <param name="tabGridLookups">The tab grid lookups.</param>
        /// <param name="value">The value.</param>
        /// <param name="beginIndex">Index of the begin.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="campoDbReport">The campo database report.</param>
        /// <returns></returns>
        private static string GetSQLWhereQuery(List<Tab_GridLookup> tabGridLookups, String value, int? beginIndex = null, int? endIndex = null, string campoDbReport = "")
        {
            //viene stratto il primo valore della tab grid look up corrispondente al nome del combo
            var mainTGL = tabGridLookups.First();

            String keyField = mainTGL.NomeCampo;
            //viene estratto il campo su cui aggiornare il db
            String dbField = campoDbReport == String.Empty ? tabGridLookups.Single(tgl => tgl.Campo_Db).NomeCampo : campoDbReport;
            //viene estratto il campo da visualizzare
            String viewField = tabGridLookups.First(tgl => tgl.Campo_Video).NomeCampo;
            //vengo estratti i campi sul quale effettuare la ricerca
            var searchFields = tabGridLookups.Where(tgl => tgl.Campo_Filtro != (int)TabGridLookupSearchFilterEnum.None).ToList();

            //risultato da inserire nella query
            StringBuilder resultSB = new StringBuilder();

            if (beginIndex == null)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (mainTGL.NomeTab == "Tab_Decod")
                    {
                        resultSB.Append(TabDecodColumnsEnum.Nome_Tab).Append(" = \"").Append(mainTGL.NomeTab_Decod).Append("\" AND ");
                    }
                    //se il campo rappresenta un ID
                    if (dbField.ToUpper().EndsWith("_ID"))
                    {
                        int idValue = -1;
                        if (Int32.TryParse(value, out idValue))
                            resultSB.Append(dbField).Append(" = ").Append(value);

                    }
                    else resultSB.Append(dbField).Append(" = \"").Append(value).Append("\"");
                }
            }
            else
            {
                if (mainTGL.NomeTab == "Tab_Decod")
                {
                    resultSB.Append(TabDecodColumnsEnum.Nome_Tab).Append(" = \"").Append(mainTGL.NomeTab_Decod).Append("\"");

                    if (!String.IsNullOrEmpty(value))
                    {
                        if (viewField.ToUpper().EndsWith("_ID"))
                            resultSB.Append(" AND ").Append(viewField).Append(" = ").Append(value);
                        else
                        {
                            resultSB.Append(" AND (");

                            if (searchFields.Count() > 0)
                            {
                                for (int i = 0; i < searchFields.Count(); i++)
                                {
                                    var item = searchFields[i];

                                    var seachType = ".Contains";
                                    if (item.Campo_Filtro == (int)TabGridLookupSearchFilterEnum.StartsWith)
                                        seachType = ".StartsWith";

                                    resultSB.Append(item.NomeCampo).Append(seachType).Append("(\"").Append(value).Append("\")");

                                    if (i + 1 < searchFields.Count())
                                        resultSB.Append(" OR ");
                                }
                            }
                            else
                                resultSB.Append(viewField).Append(".Contains(\"").Append(value).Append("\")");

                            resultSB.Append(")");
                        }
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        if (viewField.ToUpper().EndsWith("_ID"))
                            resultSB.Append(viewField).Append(" = ").Append(value);
                        else
                        {
                            if (searchFields.Count() > 0)
                            {
                                for (int i = 0; i < searchFields.Count(); i++)
                                {
                                    var item = searchFields[i];

                                    var seachType = ".Contains";
                                    if (item.Campo_Filtro == (int)TabGridLookupSearchFilterEnum.StartsWith)
                                        seachType = ".StartsWith";

                                    resultSB.Append(item.NomeCampo).Append(seachType).Append("(\"").Append(value).Append("\")");

                                    if (i + 1 < searchFields.Count())
                                        resultSB.Append(" OR ");
                                }
                            }
                            else
                                resultSB.Append(viewField).Append(".Contains(\"").Append(value).Append("\")");
                        }
                    }
                }
            }


            return resultSB.ToString();
        }

        private Dictionary<string, string> CheckConsistency(List<Tab_GridLookup> currentTGLs, Tab_GridLookup entity)
        {

            Dictionary<string, string> result = new Dictionary<string, string>();

            if (currentTGLs.Where(tgl => tgl.Campo_Db).Count() > 1)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_DB_ERRATO, PowerWebResources.FLD_NOMERICERCA));

            if (currentTGLs.Where(tgl => tgl.Campo_Db).Count() < 1)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_DB_ERRATO, PowerWebResources.FLD_NOMERICERCA));

            if (currentTGLs.Where(tgl => tgl.Campo_Video).Count() == 0)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                     BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_VIDEO_MANCANTE, PowerWebResources.FLD_NOMERICERCA));

            return result;
        }

        public override Dictionary<string, string> Check(Tab_GridLookup entity, bool isNew = false, bool isResetSession = true)
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
                //leggo i REcord già presenti per quella Ricerca
                var currentTGLs = RepoManager.Tab_GridLookupRepo.Find(tgl => tgl.NomeRicerca == entity.NomeRicerca).ToList();

                if (!isNew)
                    currentTGLs.RemoveAll(tgl => tgl.Tab_GridLookup_Id == entity.Tab_GridLookup_Id);

                currentTGLs.Add(entity);
                result = CheckConsistency(currentTGLs, entity);

                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco  
                // Chiave Logica UNIVOCA : NomeRicerca + ORDINAMENTO + NOMECAMPO
                //
                if (CommonService.Nz(entity.NomeRicerca, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOMERICERCA));
                if (CommonService.Nz(entity.Ordinamento, 0) < 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Ordinamento),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_ORDINAMENTO));
                if (CommonService.Nz(entity.NomeCampo, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeCampo),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOMECAMPO));
                if (isNew)
                {
                    if (RepoManager.Tab_GridLookupRepo.SingleOrDefault(tgl => tgl.NomeRicerca == entity.NomeRicerca &&
                                                                     tgl.Ordinamento == entity.Ordinamento &&
                                                                     tgl.Utenti_Id == entity.Utenti_Id) != null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                }
                else
                {
                    if (RepoManager.Tab_GridLookupRepo.SingleOrDefault(u => u.NomeRicerca == entity.NomeRicerca &&
                                                                    u.Ordinamento == entity.Ordinamento &&
                                                                      u.Utenti_Id == entity.Utenti_Id &&
                                                                    u.Tab_GridLookup_Id != entity.Tab_GridLookup_Id) != null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeRicerca),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                }
                // Verifico che l'Utente che sta modificando la Tabella abbia livello >=12
                if (PowerWebContext.Current.UserLevel.Funz_Aut < Common.Properties.Settings.Default.Winit_Level)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa),
                           BusinessService.GetLocalizedString(PowerWebResources.ERR_UTENTE_NON_AUTORIZZATO_TABELLA));
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //      
                if (CommonService.Nz(entity.NomeTab, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeTab),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOMETAB));
                //Controllo che il Nome della TABELLA inserita sia un Nome di una tabella esistente nel Dominio delle Tabelle gestite
                var currentType = Type.GetType(String.Format("Domain.{0}, Domain", entity.NomeTab));
                if (currentType == null)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeTab),
                     BusinessService.GetLocalizedString(PowerWebResources.ERR_TABELLA_X_NON_ESISTE, PowerWebResources.FLD_NOMETAB));
                else
                //Controllo che il Nome del Campo sia un Nome di uno dei Campi della Tabella indicata
                {
                    var property = currentType.GetProperty(entity.NomeCampo);
                    if (property == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeTab_Decod),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_NON_ESISTE_IN_TABELLA_Y,
                          PowerWebResources.FLD_NOMECAMPO, PowerWebResources.FLD_NOMETAB));
                }
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                if (CommonService.Nz(entity.Campo_Localizzato, false) == true)
                    if (CommonService.Nz(entity.Nome_Risorsa, "") == "")
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO));
                //Verifico che sia presente nelle Risorse
                //if (Common.CommonServiceBiz.GetLocalizedString(entity.Nome_Risorsa, Common.ResourceTypeEnum.None).StartsWith("### - "))
                //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa),
                //    CommonServiceBiz.GetLocalizedString(PowerWebResources.ERR_NOME_RISORSA_X_NON_ESISTE_IN_RESOURCES, entity.Nome_Risorsa));

                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.NomeCampo, "") != "")
                    if (entity.NomeCampo.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeCampo),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOMECAMPO, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.Nome_Risorsa, "") != "")
                    if (entity.Nome_Risorsa.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Risorsa),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_RISORSA, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.NomeTab, "") != "")
                    if (entity.NomeTab.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeTab),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOMETAB, PowerWebResources.VALORE_50));
                if (CommonService.Nz(entity.NomeTab_Decod, "") != "")
                    if (entity.NomeTab_Decod.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.NomeTab_Decod),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOMETAB_DECOD, PowerWebResources.VALORE_50));

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
                var CodErr = "Tab_GridLookUp_Id: " + entity.Tab_GridLookup_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckBeforeDelete(Tab_GridLookup entity)
        {
            var result = base.CheckBeforeDelete(entity);

            var currentTGLs = RepoManager.Tab_GridLookupRepo.Find(tgl => tgl.NomeRicerca == entity.NomeRicerca).ToList();

            currentTGLs.RemoveAll(tgl => tgl.Tab_GridLookup_Id == entity.Tab_GridLookup_Id);

            result = CheckConsistency(currentTGLs, entity);

            return result;
        }

        
        public static object DynamicStore<TEntity>(DataSourceLoadOptions loadOptions) where TEntity : class
        {

            RepoManager.Tab_GridLookupRepo.Context.Configuration.ProxyCreationEnabled = false;

            var result = DataSourceLoader.Load(RepoManager.Tab_GridLookupRepo.Context.Set<TEntity>().AsNoTracking(), loadOptions);

            object materializedDataSource = JObject.FromObject(result).ToObject<object>();

            RepoManager.Tab_GridLookupRepo.Context.Configuration.ProxyCreationEnabled = true;

            return materializedDataSource;
        }

        public static JObject DxLookUpStore<TEntity>(object[] whereArray, object selectArray, int skip, int take) where TEntity : class
        {
            JObject result = new JObject();

            var data = RepoManager.Tab_GridLookupRepo.Context.Set<TEntity>().DxWhere(JArray.FromObject(whereArray)).DxSelect(JArray.FromObject(selectArray)).ToList();

            var totalCount = RepoManager.Tab_GridLookupRepo.Context.Set<TEntity>().DxWhere(JArray.FromObject(whereArray)).Count();

            result.Add("data", JArray.FromObject(data));

            result.Add("totalCount", totalCount);

            return result;
        }

    }
}
