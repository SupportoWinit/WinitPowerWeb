using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Common;
using System.Collections;
using Business.MDBSchema;
using log4net;
using System.Linq.Expressions;

namespace Business.Repository.Custom
{
    public class Col_VarRepository : GenericRepository<Col_Var>, ICol_VarRepository
    {
        public Col_VarRepository(PowerWebEntities context)
            : base(context)
        {
        }

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Col_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Col_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Cant> Cants
        {
            get
            {
                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cants_Col_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_Col_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Resp> Resps
        {
            get
            {
                List<Resp> oLista = PowerWebContext.GetFromSession<List<Resp>>("Resps_Col_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.RespRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Resp>>("Resps_Col_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Col> Cols
        {
            get
            {
                List<Col> oLista = PowerWebContext.GetFromSession<List<Col>>("Cols_Col_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.ColRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Col>>("Cols_Col_VarRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Resp>>("Resps_Col_VarRepo", null);
            PowerWebContext.SetToSession<List<Cant>>("Cants_Col_VarRepo", null);
            PowerWebContext.SetToSession<List<Fil>>("Tab_Decods_Col_VarRepo", null);
            PowerWebContext.SetToSession<List<Col>>("Cols_Col_VarRepo", null);
        }

        public override Col_Var Init()
        {
            Col_Var oNewRecord = base.Init();
            oNewRecord.Assegni_Famigliari_Col = false;
            oNewRecord.Automunito_Col = false;
            oNewRecord.Disabile_Col = false;
            oNewRecord.DisAbilitazione_Col = false;
            oNewRecord.Flag_Monte_Ore = false;
            oNewRecord.NottAbi_Col = false;
            oNewRecord.Singola_Reg = false;
            oNewRecord.Straniero_CEE_Col = false;
            oNewRecord.Straniero_Col = false;
            oNewRecord.Data_Registrazione_Col = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Col = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Col_Var entity)
        {
            entity.Codice_Collaboratore = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Collaboratore, 20);
        }

        public override Dictionary<string, string> Check(Col_Var entity, bool isNew = false, bool isResetSession = true)
        {
            //i controlli su questa tabella sono ridotti al minimo (deciso insieme a Massimo il 12/09/2012)
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Il ResetSession NON SERVE perchè vengono fatti SOLO CONTROLLI LOCALI
            //if (isResetSession)
            //    ResetSession();

            //NON faccio il controllo sulla DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo
            try
            {

                if (entity.Col_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_COL_ID));                
                if (entity.Cant_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_CANT_ID));                    
                if (string.IsNullOrEmpty(entity.Codice_Collaboratore))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_COLLABORATORE));
                if (string.IsNullOrEmpty(entity.Cognome_Col))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COGNOME_COL));
                       if (string.IsNullOrEmpty(entity.Nome_Col))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_NOME_COL));
                if (entity.Monte_Ore == Int32.MinValue )
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Monte_Ore),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_MONTE_ORE));
                if (entity.Tipo_Arrotondamento_Col == Int32.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Arrotondamento_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TIPO_ARROTONDAMENTO_COL));
                if (entity.TipoNotturno_Col == Int32.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.TipoNotturno_Col),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TIPONOTTURNO_COL));
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Col_Id= " + entity.Col_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Col_Var entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (entity.Monte_Ore == Int32.MinValue)
                entity.Monte_Ore = 0;
            if (entity.Tipo_Arrotondamento_Col== Int32.MinValue )
                entity.Tipo_Arrotondamento_Col=0;
            if (entity.Metodo_Arrotondamento_Col == Int32.MinValue)
                entity.Metodo_Arrotondamento_Col = 0;
             if (entity.TipoNotturno_Col== Int32.MinValue )   
                 entity.TipoNotturno_Col=0;
             if (entity.Col_Id == Int32.MinValue)
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Col_Id),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COL_ID));
             if (String.IsNullOrEmpty(entity.Codice_Collaboratore))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Collaboratore),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_CANTIERE));
             
            //Verifico SOLO x IMPORT la Validità delle eventuali Date Ricevute
            if (entity.Data_Registrazione_Col < new DateTime(2000, 01, 01))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_COL));
             if (entity.DataOraUltimaModifica_Col < new DateTime(2000, 01, 01))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_COL));
             if (CommonService.Nz(entity.Assegni_Famigliari_Inizio_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Assegni_Famigliari_Inizio_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_INIZIO_COL));
             if (CommonService.Nz(entity.Assegni_Famigliari_Fine_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Assegni_Famigliari_Fine_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_ASSEGNI_FAMIGLIARI_FINE_COL));
             if (CommonService.Nz(entity.Data_Disponibilita_Inizio_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Inizio_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_DISPONIBILITA_INIZIO_COL));
             if (CommonService.Nz(entity.Data_Disponibilita_Fine_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Disponibilita_Fine_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_DISPONIBILITA_FINE_COL));
             if (CommonService.Nz(entity.Data_Sorv_San_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Sorv_San_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_SORV_SAN_COL));
             if (CommonService.Nz(entity.Scadenza_Patente_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Scadenza_Patente_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_SCADENZA_PATENTE_COL));
             if (CommonService.Nz(entity.Straniero_Scadenza_Permesso_Col, new DateTime(1800, 1, 1)) < new DateTime(1800, 1, 1))
                 result.AddOrAppend(CommonService.GetPropertyName(() => entity.Straniero_Scadenza_Permesso_Col),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_STRANIERO_SCADENZA_PERMESSO_COL));             
             WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Col_Var");
            List<PowerMDBDataSet.Col_VarRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.Col_VarRow>(oDataSet.Col_Var.ToList(), "Col_Var", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Col_Var> toImport = new List<Col_Var>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Col_VarRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.RRN.ToString();
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;                       
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "COL_VAR",
                            "9", "17", countRec.ToString(), countRec.ToString(), nRec.ToString()));
                            Col_Var oNewRecord = this.Init();
                            Col oCol = Cols.SingleOrDefault(c => c.Codice_Collaboratore == oRow.Codice_Collaboratore);
                            if (oCol != null)
                                oNewRecord.Col_Id = oCol.Col_Id;
                            oNewRecord.Codice_Collaboratore = oRow.Codice_Collaboratore;
                            oNewRecord.Matricola_Col = oRow.IsMatricola_ColNull() ? (string)null : oRow.Matricola_Col;
                            oNewRecord.Cognome_Col = oRow.IsCognome_ColNull() ? (string)null : oRow.Cognome_Col;
                            oNewRecord.Nome_Col = oRow.IsNome_ColNull() ? (string)null : oRow.Nome_Col;
                            if (!oRow.IsFiliale_ColNull())
                            {
                                Resp oRecord = Resps.SingleOrDefault(x => x.Codice_Resp == oRow.Filiale_Col);
                                if (oRecord != null)
                                    oNewRecord.Resp_Id = oRecord.Resp_Id;
                            }
                            if (!oRow.IsCodice_Cantiere_ColNull())
                            {
                                Cant oRecord = Cants.SingleOrDefault(x => x.Codice_Cantiere == oRow.Codice_Cantiere_Col);
                                if (oRecord != null)
                                    oNewRecord.Cant_Id = oRecord.Cant_Id;
                            }
                            //TODO Ste - gestione Tipo_Arrotondamento_Col???
                            oNewRecord.Data_Registrazione_Col = oRow.IsData_Registrazione_ColNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_Col;
                            oNewRecord.DataOraUltimaModifica_Col = oRow.IsDataOraUtimaModificaNull() ? new DateTime(2000, 1, 1) : oRow.DataOraUtimaModifica;
                            oNewRecord.DisAbilitazione_Col = oRow.IsDisAbilitazione_ColNull() ? false : oRow.DisAbilitazione_Col;
                            oNewRecord.Data_Disponibilita_Inizio_Col = oRow.IsData_Disponibilità_Inizio_ColNull() ? (DateTime?)null : (DateTime)oRow.Data_Disponibilità_Inizio_Col;
                            oNewRecord.Data_Disponibilita_Fine_Col = oRow.IsData_Disponibilità_Fine_ColNull() ? (DateTime?)null : (DateTime)oRow.Data_Disponibilità_Fine_Col;
                            oNewRecord.Nascita_Data_Col = oRow.IsNascita_Data_ColNull() ? (DateTime?)null : (DateTime)oRow.Nascita_Data_Col;
                            oNewRecord.Nascita_Luogo_Col = oRow.IsNascita_Luogo_ColNull() ? (string)null : oRow.Nascita_Luogo_Col;
                            oNewRecord.Nazionalita_Col = oRow.IsNazionalita_ColNull() ? (string)null : oRow.Nazionalita_Col;
                            oNewRecord.Straniero_Col = oRow.IsStraniero_ColNull() ? false : oRow.Straniero_Col;
                            oNewRecord.Straniero_CEE_Col = oRow.IsStraniero_CEE_ColNull() ? false : oRow.Straniero_CEE_Col;
                            oNewRecord.Straniero_Scadenza_Permesso_Col = oRow.IsStraniero_Scadenza_Permesso_ColNull() ? (DateTime?)null : (DateTime)oRow.Straniero_Scadenza_Permesso_Col;
                            oNewRecord.Residenza_Luogo_Col = oRow.IsResidenza_Luogo_ColNull() ? (string)null : oRow.Residenza_Luogo_Col;
                            oNewRecord.Residenza_Indirizzo_Col = oRow.IsResidenza_Indirizzo_ColNull() ? (string)null : oRow.Residenza_Indirizzo_Col;
                            oNewRecord.Residenza_Provincia_Col = oRow.IsResidenza_Provincia_ColNull() ? (string)null : oRow.Residenza_Provincia_Col;
                            oNewRecord.Residenza_Cap_Col = oRow.IsResidenza_Cap_ColNull() ? (string)null : oRow.Residenza_Cap_Col;
                            oNewRecord.Domicilio_Luogo_Col = oRow.IsDomicilio_Luogo_ColNull() ? (string)null : oRow.Domicilio_Luogo_Col;
                            oNewRecord.Domicilio_Indirizzo_Col = oRow.IsDomicilio_Indirizzo_ColNull() ? (string)null : oRow.Domicilio_Indirizzo_Col;
                            oNewRecord.Domicilio_Provincia_Col = oRow.IsDomicilio_Provincia_ColNull() ? (string)null : oRow.Domicilio_Provincia_Col;
                            oNewRecord.Domicilio_Cap_Col = oRow.IsDomicilio_Cap_ColNull() ? (string)null : oRow.Domicilio_Cap_Col;
                            oNewRecord.Telefono_1_Col = oRow.IsTelefono_1_ColNull() ? (string)null : oRow.Telefono_1_Col;
                            oNewRecord.Telefono_1_Rif_Col = oRow.IsTelefono_1_Rif_ColNull() ? (string)null : oRow.Telefono_1_Rif_Col;
                            oNewRecord.Telefono_2_Col = oRow.IsTelefono_2_ColNull() ? (string)null : oRow.Telefono_2_Col;
                            oNewRecord.Telefono_2_Rif_Col = oRow.IsTelefono_2_Rif_ColNull() ? (string)null : oRow.Telefono_2_Rif_Col;
                            oNewRecord.Telefono_3_Col = oRow.IsTelefono_3_ColNull() ? (string)null : oRow.Telefono_3_Col;
                            oNewRecord.Telefono_3_Rif_Col = oRow.IsTelefono_3_Rif_ColNull() ? (string)null : oRow.Telefono_3_Rif_Col;
                            oNewRecord.Telefono_4_Col = oRow.IsTelefono_4_ColNull() ? (string)null : oRow.Telefono_4_Col;
                            oNewRecord.Telefono_4_Rif_Col = oRow.IsTelefono_4_Rif_ColNull() ? (string)null : oRow.Telefono_4_Rif_Col;
                            oNewRecord.Fax_1_Col = oRow.IsFax_1_ColNull() ? (string)null : oRow.Fax_1_Col;
                            oNewRecord.Fax_1_Rif_Col = oRow.IsFax_1_Rif_ColNull() ? (string)null : oRow.Fax_1_Rif_Col;
                            oNewRecord.Fax_2_Col = oRow.IsFax_2_ColNull() ? (string)null : oRow.Fax_2_Col;
                            oNewRecord.Fax_2_Rif_Col = oRow.IsFax_2_Rif_ColNull() ? (string)null : oRow.Fax_2_Rif_Col;
                            oNewRecord.Qualifica_Col = oRow.IsQualifica_ColNull() ? (string)null : oRow.Qualifica_Col;
                            oNewRecord.Tipo_Col = oRow.IsTipo_ColNull() ? (string)null : oRow.Tipo_Col;
                            oNewRecord.Tipo_Rapporto_Col = oRow.IsTipo_Rapporto_ColNull() ? (string)null : oRow.Tipo_Rapporto_Col;
                            oNewRecord.Livello_Col = oRow.IsLivello_ColNull() ? (string)null : oRow.Livello_Col;
                            oNewRecord.Retribuzione_Oraria_Col = oRow.IsRetribuzione_Oraria_ColNull() ? (double?)null : (double)oRow.Retribuzione_Oraria_Col;
                            oNewRecord.Indennita_Sanificazione_Col = oRow.IsIndennità_Sanificazione_ColNull() ? (double?)null : (double)oRow.Indennità_Sanificazione_Col;
                            oNewRecord.Indennita_Trasporto_Col = oRow.IsIndennità_Trasporto_ColNull() ? (double?)null : (double)oRow.Indennità_Trasporto_Col;
                            oNewRecord.Assegni_Famigliari_Col = oRow.IsAssegni_Famigliari_ColNull() ? false : oRow.Assegni_Famigliari_Col;
                            oNewRecord.Assegni_Famigliari_Inizio_Col = oRow.IsAssegni_Famigliari_Inizio_ColNull() ? (DateTime?)null : (DateTime)oRow.Assegni_Famigliari_Inizio_Col;
                            oNewRecord.Assegni_Famigliari_Fine_Col = oRow.IsAssegni_Famigliari_Fine_ColNull() ? (DateTime?)null : (DateTime)oRow.Assegni_Famigliari_Fine_Col;
                            oNewRecord.Stato_Civile_Col = oRow.IsStato_Civile_ColNull() ? (string)null : oRow.Stato_Civile_Col;                            
                            oNewRecord.Sesso_Col = (oRow.IsSesso_ColNull() ? (string)null
                             : (oRow.Sesso_Col == "M" ? PowerWebResources.TD_SESSO_MASCHIO.ToString()
                             : (oRow.Sesso_Col == "F" ? PowerWebResources.TD_SESSO_FEMMINA.ToString()
                             : (string)null)));
                            oNewRecord.Titolo_Studio_Col = oRow.IsTitolo_Studio_ColNull() ? (string)null : oRow.Titolo_Studio_Col;
                            oNewRecord.Automunito_Col = oRow.IsAutomunito_ColNull() ? false : oRow.Automunito_Col;
                            oNewRecord.Patente_Col = oRow.IsPatente_ColNull() ? (string)null : oRow.Patente_Col;
                            oNewRecord.Libretto_Sanitario_Col = oRow.IsLibretto_Sanitario_ColNull() ? (string)null : oRow.Libretto_Sanitario_Col;
                            oNewRecord.Codice_Fiscale_Col = oRow.IsCodice_Fiscale_ColNull() ? (string)null : oRow.Codice_Fiscale_Col;
                            oNewRecord.Note_Col = oRow.IsNote_ColNull() ? (string)null : oRow.Note_Col;
                            oNewRecord.Durata_Min_Ril_Col = oRow.IsDurata_Min_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Min_Ril_Col.TimeOfDay.Ticks);
                            oNewRecord.Durata_Max_Ril_Col = oRow.IsDurata_Max_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Ril_Col.TimeOfDay.Ticks);
                            oNewRecord.Durata_Max_Gruppo_Ril_Col = oRow.IsDurata_Max_Gruppo_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Ril_Col.TimeOfDay.Ticks);
                            oNewRecord.Durata_Max_Gruppo_Notte_Ril_Col = oRow.IsDurata_Max_Gruppo_Notte_Ril_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Notte_Ril_Col.TimeOfDay.Ticks);
                            oNewRecord.Flag_NON_Esportare_Col = oRow.IsFlag_NON_Esportare_ColNull() ? (byte?)null : (byte)oRow.Flag_NON_Esportare_Col;
                            oNewRecord.Durata_Pausa_Col = oRow.IsDurata_Pausa_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Pausa_Col.TimeOfDay.Ticks);
                            oNewRecord.Flag_Ore_Viaggi_Col = oRow.IsFlag_Ore_Viaggi_ColNull() ? (byte?)null : (byte)oRow.Flag_Ore_Viaggi_Col;
                            oNewRecord.Fascia_Ore_Viaggi_1_Inizio_Col = oRow.IsFascia_Ore_Viaggi_1_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_1_Inizio_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_1_Fine_Col = oRow.IsFascia_Ore_Viaggi_1_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_1_Fine_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_2_Inizio_Col = oRow.IsFascia_Ore_Viaggi_2_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_2_Inizio_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_2_Fine_Col = oRow.IsFascia_Ore_Viaggi_2_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_2_Fine_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_3_Inizio_Col = oRow.IsFascia_Ore_Viaggi_3_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_3_Inizio_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_3_Fine_Col = oRow.IsFascia_Ore_Viaggi_3_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_3_Fine_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_4_Inizio_Col = oRow.IsFascia_Ore_Viaggi_4_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_4_Inizio_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_4_Fine_Col = oRow.IsFascia_Ore_Viaggi_4_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_4_Fine_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_5_Inizio_Col = oRow.IsFascia_Ore_Viaggi_5_Inizio_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_5_Inizio_COL.TimeOfDay.Ticks);
                            oNewRecord.Fascia_Ore_Viaggi_5_Fine_Col = oRow.IsFascia_Ore_Viaggi_5_Fine_COLNull() ? (TimeSpan?)null : new TimeSpan(oRow.Fascia_Ore_Viaggi_5_Fine_COL.TimeOfDay.Ticks);
                            oNewRecord.Soglia_Arrot_Durata_Fig_Col = oRow.IsSoglia_Arrot_Durata_Fig_ColNull() ? (short?)null : (short)oRow.Soglia_Arrot_Durata_Fig_Col;
                            oNewRecord.Minuti_Arrot_Durata_Fig_Col = oRow.IsMinuti_Arrot_Durata_Fig_ColNull() ? (short?)null : (short)oRow.Minuti_Arrot_Durata_Fig_Col;
                            oNewRecord.TipoNotturno_Col = oRow.IsTipoNotturno_ColNull() ? (int)0 : Int32.Parse(oRow.TipoNotturno_Col);
                            oRow.TipoNotturno_Col = oRow.IsTipoNotturno_ColNull() ? "0" : oRow.TipoNotturno_Col;    
                            if (!oRow.IsFlagCambioGiornoRil_ColNull() && oRow.FlagCambioGiornoRil_Col == true)
                            {
                                if (!oRow.IsFlagCambioGiornoRil_ColNull() && oRow.TipoNotturno_Col == "1")
                                    oNewRecord.TipoNotturno_Col = 2;
                                else
                                    oNewRecord.TipoNotturno_Col = 1;
                            }
                            else
                                oNewRecord.TipoNotturno_Col = 0;
                            oNewRecord.Tipo_Contratto_Col = oRow.IsTipoContrattoNull() ? (string)null : oRow.TipoContratto;
                            oNewRecord.Prova = oRow.IsProvaNull() ? (byte?)null : (byte)oRow.Prova;
                            oNewRecord.OreMassime = oRow.IsOreMassimeNull() ? (short?)null : (short)oRow.OreMassime;
                            oNewRecord.Singola_Reg = oRow.IsSingola_RegNull() ? false : oRow.Singola_Reg;
                            oNewRecord.Limite_Inizio_Notte_Col = oRow.IsLimite_Inizio_Notte_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.Limite_Inizio_Notte_Col.TimeOfDay.Ticks);
                            oNewRecord.NottAbi_Col = oRow.IsNottAbi_ColNull() ? false : oRow.NottAbi_Col;
                            oNewRecord.OreMaxGG_Col = oRow.IsOreMaxGG_ColNull() ? (TimeSpan?)null : new TimeSpan(oRow.OreMaxGG_Col.TimeOfDay.Ticks);
                            oNewRecord.SogliaI_Col = oRow.IsSogliaI_ColNull() ? (short?)null : (short)oRow.SogliaI_Col;
                            oNewRecord.SogliaF_Col = oRow.IsSogliaF_ColNull() ? (short?)null : (short)oRow.SogliaF_Col;
                            oNewRecord.ArrotI_Col = oRow.IsArrotI_ColNull() ? (short?)null : (short)oRow.ArrotI_Col;
                            oNewRecord.ArrotF_Col = oRow.IsArrotF_ColNull() ? (short?)null : (short)oRow.ArrotF_Col;
                            oNewRecord.Arrot_Durata_Col = oRow.IsArrot_Durata_ColNull() ? (short?)null : (short)oRow.Arrot_Durata_Col;
                            oNewRecord.Soglia_Durata_Col = oRow.IsSoglia_Durata_ColNull() ? (short?)null : (short)oRow.Soglia_Durata_Col;
                            oNewRecord.GGConsMax_Col = oRow.IsGGConsMax_ColNull() ? (byte?)null : (byte)oRow.GGConsMax_Col;
                            oNewRecord.Metodo_Arrotondamento_Col = oRow.IsMetodo_Arrotondamento_ColNull() ? (byte?)null : (byte)oRow.Metodo_Arrotondamento_Col;
                            oNewRecord.Nascita_Provincia_Col = oRow.IsNascita_Provincia_ColNull() ? (string)null : oRow.Nascita_Provincia_Col;
                            oNewRecord.Residenza_Localita_Col = oRow.IsResidenza_Localita_ColNull() ? (string)null : oRow.Residenza_Localita_Col;
                            oNewRecord.Residenza_Interno_Col = oRow.IsResidenza_Interno_ColNull() ? (string)null : oRow.Residenza_Interno_Col;
                            oNewRecord.Codice_Nascita_Luogo_Col = oRow.IsCodice_Nascita_Luogo_ColNull() ? (string)null : oRow.Codice_Nascita_Luogo_Col;
                            oNewRecord.Codice_Residenza_Luogo_Col = oRow.IsCodice_Residenza_Luogo_ColNull() ? (string)null : oRow.Codice_Residenza_Luogo_Col;
                            oNewRecord.Domicilio_Interno_Col = oRow.IsDomicilio_Interno_ColNull() ? (string)null : oRow.Domicilio_Interno_Col;
                            oNewRecord.Domicilio_Localita_Col = oRow.IsDomicilio_Localita_ColNull() ? (string)null : oRow.Domicilio_Localita_Col;
                            oNewRecord.Codice_Domicilio_Luogo_Col = oRow.IsCodice_Domicilio_Luogo_ColNull() ? (string)null : oRow.Codice_Domicilio_Luogo_Col;
                            oNewRecord.Nascita_Cap_Col = oRow.IsNascita_Cap_ColNull() ? (string)null : oRow.Nascita_Cap_Col;
                            oNewRecord.Disabile_Col = false;//campo non esistente in Access
                            oNewRecord.Retribuzione_Straordinaria_Col = oRow.IsRetribuzione_Straordinaria_ColNull() ? (double?)null : (double)oRow.Retribuzione_Straordinaria_Col;
                            oNewRecord.Scadenza_Patente_Col = oRow.IsScadenza_Patente_ColNull() ? (DateTime?)null : (DateTime)oRow.Scadenza_Patente_Col;
                            oNewRecord.N_Pos_INPS_Col = oRow.IsN_Pos_INPS_ColNull() ? (string)null : oRow.N_Pos_INPS_Col;
                            oNewRecord.N_Pos_INAIL_Col = oRow.IsN_Pos_INPS_ColNull() ? (string)null : oRow.N_Pos_INPS_Col;
                            oNewRecord.Retribuzione_Netta_Col = oRow.IsRetribuzione_Netta_ColNull() ? (double?)null : (double)oRow.Retribuzione_Netta_Col;
                            oNewRecord.Retribuzione_Lorda_Col = oRow.IsRetribuzione_Lorda_ColNull() ? (double?)null : (double)oRow.Retribuzione_Lorda_Col;
                            oNewRecord.Trattenuta_Vitto_Col = oRow.IsTrattenuta_Vitto_ColNull() ? (double?)null : (double)oRow.Trattenuta_Vitto_Col;
                            oNewRecord.Codice_Iban_Col = oRow.IsCodice_Iban_ColNull() ? (string)null : oRow.Codice_Iban_Col;
                            oNewRecord.Quota_PTime_Col = oRow.IsQuota_PTime_ColNull() ? (float?)null : (float)oRow.Quota_PTime_Col;
                            oNewRecord.N_Persone_A_Carico_Col = oRow.IsN_Persone_A_Carico_ColNull() ? (double?)null : (double)oRow.N_Persone_A_Carico_Col;
                            oNewRecord.Residenza_Provincia_GEN_Col = oRow.IsResidenza_Provincia_GEN_ColNull() ? (string)null : oRow.Residenza_Provincia_GEN_Col;
                            oNewRecord.Flag_INPS_Col = oRow.IsFlag_INPS_ColNull() ? (string)null : oRow.Flag_INPS_Col;
                            oNewRecord.Data_Sorv_San_Col = oRow.IsData_Sorv_San_ColNull() ? (DateTime?)null : oRow.Data_Sorv_San_Col;
                            oNewRecord.Limite_Entrata_Mattina_Col = oRow.IsLimite_Entrata_ColNull() ? (TimeSpan?)null : oRow.Limite_Entrata_Col.TimeOfDay;
                            //TODO Ste - da gestire
                            oNewRecord.Raggruppamento1_Col = "";
                            oNewRecord.Raggruppamento2_Col = "";

                            // Eseguo i Controlli Specifici della CheckForImport e poi i Controlli Standard della Check        
                            Dictionary<string, string> importDictionary = CheckForImport(oNewRecord);
                            //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                            if (countRec == 1)
                                currentDictionary = Check(oNewRecord, true, true);
                            else
                                currentDictionary = Check(oNewRecord, true, false);                              
                        
                            //Verifico se ci sono stati errori
                            if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                                toImport.Add(oNewRecord);
                            else
                            {
                                errors.Add(new Tab_Chk_Imp
                                {
                                    Nome_Tabella_Tab_Check_Imp = "Col_Var",
                                    Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                                });
                                log.Warn("Codice Collaboratore cui si rifericono gli errori precedenti: " + oRow.Codice_Collaboratore + " - RRN: " + oRow.RRN +  " -------------------------------------------------------------------------------");
                                errorsList.Add(currentDictionary);
                                errorsList.Add(importDictionary);
                            }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.RRN;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB COL_VAR - Chiave: {0}", oRow.RRN.ToString());
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                        BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "COL_VAR", lastKey));
                    RepoManager.Col_VarRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Col_Var",
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

        public override Expression<Func<Col_Var, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Resp) == DomainFilterEnum.Resp && PowerWebContext.Current.Resps != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allColIds = PowerWebContext.Current.ColsIds;
                    return colVar => allColIds.Contains(colVar.Col_Id);
                }
                else return base.Filter;
            }
        }
    }
}

