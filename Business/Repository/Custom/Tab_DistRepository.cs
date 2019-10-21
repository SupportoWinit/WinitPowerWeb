using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;

namespace Business.Repository.Custom
{
    public class Tab_DistRepository : GenericRepository<Tab_Dist>, ITab_DistRepository
    {
        public Tab_DistRepository(PowerWebEntities context)
            : base(context)
        {
        }

        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Tab_DistRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Tab_DistRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Comuni> Tab_Comunis
        {
            get
            {
                List<Tab_Comuni> oLista = PowerWebContext.GetFromSession<List<Tab_Comuni>>("Tab_Comunis_Tab_DistRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ComuniRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_Tab_DistRepo", oLista);
                }
                return oLista;
            }
        }

      



        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Tab_DistRepo", null);
            PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_Tab_DistRepo", null);
        }

        public override Dictionary<string, string> Check(Tab_Dist entity, bool isNew = false, bool isResetSession = true)
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
                if (CommonService.Nz(entity.Tab_Decod_Id, 0) == 0 || CommonService.Nz(entity.Arrivo_Tab_Dist, "") == "" ||
                    CommonService.Nz(entity.Partenza_Tab_Dist, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Dist_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_DATI_NECESSARI_MANCANTI));
                else
                {
                    if (isNew)
                    {
                        if (RepoManager.Tab_DistRepo.SingleOrDefault(u => u.Tab_Decod_Id == entity.Tab_Decod_Id &&
                           u.Partenza_Tab_Dist == entity.Partenza_Tab_Dist && u.Arrivo_Tab_Dist == entity.Arrivo_Tab_Dist) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Dist_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.Tab_DistRepo.SingleOrDefault(u => u.Tab_Decod_Id == entity.Tab_Decod_Id && u.Tab_Dist_Id != entity.Tab_Dist_Id &&
                          u.Partenza_Tab_Dist == entity.Partenza_Tab_Dist && u.Arrivo_Tab_Dist == entity.Arrivo_Tab_Dist) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Dist_Id),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }//
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (CommonService.Nz(entity.Tab_Decod_Id, 0) == 0)
                    if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_SYS.ToString()
                    && x.Nome_Tab == TabDecodNameEnum.TIPO_DISTANZA.ToString() && x.Chiave_Tab == entity.Tab_Decod_Id.ToString()) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_TIPO_ARRIVO_TAB_DISTANZE));

                if (CommonService.Nz(entity.Partenza_Tab_Dist, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                         PowerWebResources.FLD_PARTENZA_TAB_DIST));

                if (CommonService.Nz(entity.Arrivo_Tab_Dist, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
                         BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                         PowerWebResources.FLD_ARRIVO_TAB_DIST));

                //if (CommonService.Nz(entity.Minuti_Tab_Dist, 0) == 0)
                //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Minuti_Tab_Dist),
                //         BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                //         PowerWebResources.FLD_MINUTI_TAB_DIST));

                //if (CommonService.Nz(entity.KM_Tab_Dist, 0) == 0)
                //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.KM_Tab_Dist),
                //         BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                //         PowerWebResources.FLD_KM_TAB_DIST));

                //Recupero dalla tab.Decod l'indice da utilizzare per i Vari Tipi di Partenza/Arrivo
                // si forza l'id da controllare nella linq a stringa in quanto non è possibile utilizzare il metodo tostring() all'interno di una linq
                var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Tab_Decod_Id == entity.Tab_Decod_Id);

                if (tabDecod != null)
                {
                    if (tabDecod.Chiave_Tab == "C")
                    //In questo caso si tratta di Partenza/arrivo da un Cantiere all'altro
                    {
                        var idPartenza = Convert.ToInt32(entity.Partenza_Tab_Dist);
                        var idArrivo = Convert.ToInt32(entity.Arrivo_Tab_Dist);

                        if (RepoManager.CantRepo.SingleOrDefault(x => x.Cant_Id == idPartenza) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_PARTENZA_TAB_DIST, PowerWebResources.STR_CANTIERI));
                        if (RepoManager.CantRepo.SingleOrDefault(x => x.Cant_Id == idArrivo) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_ARRIVO_TAB_DIST, PowerWebResources.STR_CANTIERI));
                    }
                    if (tabDecod.Chiave_Tab == "K")
                    //In questo caso si tratta di Partenza/arrivo da un CAP all'altro                    
                    {
                        if (Tab_Comunis.FirstOrDefault(u => u.Cap_Tab_Comuni == entity.Partenza_Tab_Dist) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_PARTENZA_TAB_DIST, PowerWebResources.STR_TAB_COMUNI));
                        if (Tab_Comunis.FirstOrDefault(u => u.Cap_Tab_Comuni == entity.Arrivo_Tab_Dist) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_ARRIVO_TAB_DIST, PowerWebResources.STR_TAB_COMUNI));
                    }
                    if (tabDecod.Chiave_Tab == "P")
                    //In questo caso si tratta di Partenza/arrivo da un LUOGO_CAN (COMUNE) all'altro                    
                    {
                     //controllo se ho abilitato il controllo della tab comuni
                        var controlloTabComuni = RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni;
                       //se ho il controllo della tab comune attivato allora non posso inserire i comuni che voglio ma controllo la presenza dei comuni in tabella
                        if (controlloTabComuni == 1)
                        {
                            if (Tab_Comunis.FirstOrDefault(u => u.Luogo_Tab_Comuni == entity.Partenza_Tab_Dist) == null)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                                    BusinessService.GetLocalizedString(
                                        PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                        PowerWebResources.FLD_PARTENZA_TAB_DIST, PowerWebResources.STR_TAB_COMUNI));
                            if (Tab_Comunis.FirstOrDefault(u => u.Luogo_Tab_Comuni == entity.Arrivo_Tab_Dist) == null)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
                                    BusinessService.GetLocalizedString(
                                        PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                        PowerWebResources.FLD_ARRIVO_TAB_DIST, PowerWebResources.STR_TAB_COMUNI));
                        }
                    }
                    if (tabDecod.Chiave_Tab == "Z")
                    //In questo caso si tratta di Partenza/arrivo da una ZONA all'altra
                    {
                        if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                                 && x.Nome_Tab == TabDecodNameEnum.ZONE.ToString() && x.Chiave_Tab == entity.Partenza_Tab_Dist) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_PARTENZA_TAB_DIST, PowerWebResources.STR_TABELLA_DECODIFICHE));
                        if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                                 && x.Nome_Tab == TabDecodNameEnum.ZONE.ToString() && x.Chiave_Tab == entity.Arrivo_Tab_Dist) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_ARRIVO_TAB_DIST, PowerWebResources.STR_TABELLA_DECODIFICHE));
                    }

                    if (tabDecod.Chiave_Tab == "G")
                    //In questo caso si tratta di Partenza/Arrivo da un COMUNE/INDIRIZZO/CAP all'altro 
                    //Questo Tipo di Record vengono generati SOLO via GIS e NON Manualmente
                    //NON possonoe essere INSERITI MANUALMENTE da VIDEO DEI NUOVI RECORD 
                    //NON SI POSSONO VARIARE MANUALMENTE da VIDEO I Valori dei Campi di PARTENZA ED ARRIVO 
                    //per evitare che i Dati non corrispondano a quelli presenti sui corrispondenti REcord dei Cantieri
                    {
                        
                        #region check Partenza_Tab_Dist / Arrivo_Tab_Dist
                       
                        if (string.IsNullOrEmpty(entity.Partenza_Tab_Dist))
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_PARTENZA_INCOMPLETI));
                        else
                        {
                            var partenzaSplitted = entity.Partenza_Tab_Dist.Split('|');
                            if (partenzaSplitted.Count() != 3)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_PARTENZA_INCOMPLETI));

                            else if (partenzaSplitted.Any(s => s.Length == 0))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_PARTENZA_INCOMPLETI));

                            else if (partenzaSplitted[2].Length != 5)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_PARTENZA_CAP_ANOMALO));
                        }

                        if (string.IsNullOrEmpty(entity.Arrivo_Tab_Dist))
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_ARRIVO_INCOMPLETI));
                        else
                        {
                            var arrivoSplitted = entity.Arrivo_Tab_Dist.Split('|');
                            if (arrivoSplitted.Count() != 3)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_ARRIVO_INCOMPLETI));

                            else if (arrivoSplitted.Any(s => s.Length == 0))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_ARRIVO_INCOMPLETI));

                            else if (arrivoSplitted[2].Length != 5)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Arrivo_Tab_Dist), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_DATI_ARRIVO_CAP_ANOMALO));
                        }

                        #endregion
                        
                        if (isNew)
                        {
                            //    //Nel caso di "G" NON si può inserire a mano un Nuovo Record
                            //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partenza_Tab_Dist),
                            //        BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_NO_INSERIMENTO_MANUALE));
                        }
                        else
                        //Nel caso di "G" se NON è un Nuovo Record verifico che NON siano stati cambiati i Luogo_partenza/Luogo_Arrivo
                        {
                            Tab_Dist tab_Dist_Old = RepoManager.Tab_DistRepo.SingleOrDefault(u => u.Tab_Dist_Id == entity.Tab_Dist_Id);
                            if (tab_Dist_Old.Arrivo_Tab_Dist != entity.Arrivo_Tab_Dist || tab_Dist_Old.Partenza_Tab_Dist != entity.Partenza_Tab_Dist)
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Dist_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_TAB_DIST_G_NO_MODIFICA_ARRIVO_PARTENZA));
                        }
                    }
                }
                else
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tab_Decod_Id),
                               BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                               PowerWebResources.FLD_TAB_DIST_ID, PowerWebResources.STR_TABELLA_DECODIFICHE));
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Tab_Dist_Id: " + entity.Tab_Dist_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Tab_Dist entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();


            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override int SaveChanges()
        {
            var a = Context.ChangeTracker.Entries<Reg>().Where(reg => reg.Entity.Col_Id == null || reg.Entity.Cant_Id == null).ToList();

            return base.SaveChanges();
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();

            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();

            List<PowerMDBDataSet.Tab_DistRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData<PowerMDBDataSet.Tab_DistRow>(
            oDataSet.Tab_Dist.ToList(), "Tab_Dist", "RRN");
            List<Tab_Dist> toImport = new List<Tab_Dist>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();

            /*
                         ILog log = LogManager.GetLogger("Pru_Col");
            List<PowerMDBDataSet.Pru_ColRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.Pru_ColRow>(oDataSet.Pru_Col.ToList(), "Pru_Col", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Pru_Col> toImport = new List<Pru_Col>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
             */

            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                string TipoRecord = null;
                foreach (PowerMDBDataSet.Tab_DistRow oRow in accessData)
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "TAB_DIST",
                        "16", "17", oRow.Arrivo_Tab_Dist, countRec.ToString(), nRec.ToString()));

                    Tab_Dist oNewRecord = this.Init();

                    //verifico che il Tipo Partenza sia Uguale al Tipo Arrivo
                    if (oRow.Tipo_Partenza_Tab_Dist != oRow.Tipo_Arrivo_Tab_Dist)
                        TipoRecord = "0";  //ERRATO (scartato dalla Check                            
                    if (oRow.Tipo_Partenza_Tab_Dist == "A")
                        // Se TAB_DIST ACCESS aveva Tipo_Partenza/Arrivo="A" allora imposto "C"=CANTIERE
                        TipoRecord = "C";
                    else if (oRow.Tipo_Partenza_Tab_Dist == "N")
                        // Se TAB_DIST ACCESS aveva Tipo_Partenza/Arrivo="N" allora imposto "K"=CAP
                        TipoRecord = "K";
                    else if (oRow.Tipo_Partenza_Tab_Dist == "C")
                        // Se TAB_DIST ACCESS aveva Tipo_Partenza/Arrivo="C" allora imposto "P"= LUOGO CANTIERE (COMUNE)
                        TipoRecord = "P";
                    else if (oRow.Tipo_Partenza_Tab_Dist == "G")
                        // Se TAB_DIST ACCESS aveva Tipo_Partenza/Arrivo="G" allora imposto "G"= GIS (CAP|LUOGO|INDIRIZZO)
                        TipoRecord = "G";

                    //Recupero da Tab_Decod l'Id corrispodnente al TIPO DI PARTENZA/ARRIVO da registrare sulla TAB_DISTANZE
                    var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == TipoRecord);
                    if (tabDecod != null)
                        oNewRecord.Tab_Decod_Id = tabDecod.Tab_Decod_Id;
                    else
                        //Se NON trovato lo imposto a Zero così poi la Check lo scarta
                        oNewRecord.Tab_Decod_Id = 0;
                    oNewRecord.Arrivo_Tab_Dist = oRow.Arrivo_Tab_Dist;
                    oNewRecord.Partenza_Tab_Dist = oRow.Partenza_Tab_Dist;
                    oNewRecord.KM_Tab_Dist = (decimal)(oRow.IsKM_Tab_DistNull() ? (double)0 : oRow.KM_Tab_Dist);
                    // le ore di access (in ore,minuti in sessantesimi) e vengono convertiti in minuti
                    oNewRecord.Minuti_Tab_Dist = oRow.IsOre_Tab_DistNull() ? (int)0 : CommonService.FromHoursToMinutes(oRow.Ore_Tab_Dist, false);
                    Dictionary<string, string> currentDictionary = Check(oNewRecord, true);
                    if (currentDictionary.Keys.Count == 0)
                        toImport.Add(oNewRecord);
                    else
                    {
                        errors.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Tab_Dist",
                            Chiave_Record_Tab_Check_Imp = oNewRecord.Tab_Dist_Id.ToString(),
                        });
                        errorsList.Add(currentDictionary);
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    RepoManager.Tab_DistRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)

                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Tab_Dist",
                            Stato_Record_Tab_Check_Imp = true,
                        });
                    RepoManager.Tab_Chk_ImpRepo.SaveChanges();
                    RepoManager.Tab_Chk_ImpRepo.CommitWork();
                    ResetSession();
                }
                catch (Exception ex)
                {
                    RepoManager.Tab_Chk_ImpRepo.RollbackWork();
                    ResetSession();
                    throw ex;
                }
            }
            return errorsList;
        }

        private void CheckPartenza_Tab_Dist(Tab_Dist entity, ref Dictionary<string, string> result)
        {

        }

    }
}

