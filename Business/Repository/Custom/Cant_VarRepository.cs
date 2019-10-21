using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using Business.MDBSchema;
using log4net;
using System.Linq.Expressions;

namespace Business.Repository.Custom
{
    public class Cant_VarRepository : GenericRepository<Cant_Var>, ICant_VarRepository
    {
        public Cant_VarRepository(PowerWebEntities context)
            : base(context)
        {
        }

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Cli> Clis
        {
            get
            {
                List<Cli> oLista = PowerWebContext.GetFromSession<List<Cli>>("Clis_Cant_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CliRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cli>>("Clis_Cant_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Fil> Fils
        {
            get
            {
                List<Fil> oLista = PowerWebContext.GetFromSession<List<Fil>>("Fils_Cant_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.FilRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Fil>>("Fils_Cant_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Cant_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Cant_VarRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Cant> Cants
        {
            get
            {
                List<Cant> oLista = PowerWebContext.GetFromSession<List<Cant>>("Cants_Cant_VarRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.CantRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Cant>>("Cants_Cant_VarRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Cli>>("Clis_Cant_VarRepo", null);
            PowerWebContext.SetToSession<List<Fil>>("Fils_Cant_VarRepo", null);
            PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Cant_VarRepo", null);
            PowerWebContext.SetToSession<List<Cant>>("Cants_Cant_VarRepo", null);
        }

        public override Cant_Var Init()
        {
            Cant_Var currentCant_Var = base.Init();
            currentCant_Var.DisAbilitazione_Can = false;
            currentCant_Var.Mensa_Can = false;
            currentCant_Var.Singola_Reg = false;
            currentCant_Var.Data_Registrazione_Can = DateTime.UtcNow;
            currentCant_Var.DataOraUltimaModifica_Can = DateTime.UtcNow;
            currentCant_Var.Tipologia_Can = "Can";
            currentCant_Var.DisAbilitazione_Can = false;
            currentCant_Var.FlagGps_Can = 0;
            currentCant_Var.Mensa_Can = false;
            currentCant_Var.Singola_Reg = false;
            currentCant_Var.Tipo_Arrotondamento_Can = 0;
            currentCant_Var.TipoNotturno_Can = 0;
            return currentCant_Var;
        }

        public override void SetEntityBeforeAddOrUpdate(Cant_Var entity)
        {
            entity.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Cantiere, 20);
            //nel caso degli assistiti allora forzo la descrizione cantiere uguale al valore dei campi cognome e nome assistito
            if (String.Compare((entity.Tipologia_Can ?? "").ToUpper(), "ASS", false) == 0)
                entity.Descrizione_Can = new StringBuilder(entity.Cognome_Assistito_Can).Append(" ").Append(entity.Nome_Assistito_Can).ToString().Trim();
        }

        public override Dictionary<string, string> Check(Cant_Var entity, bool isNew = false, bool isResetSession = true)
        {

            Dictionary<string, string> result = new Dictionary<string, string>();

            //Il ResetSession NON SERVE perchè vengono fatti SOLO CONTROLLI LOCALI
            //if (isResetSession)
            //    ResetSession();

            //NON faccio il controllo sulla DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo
            try
            {
                //i controlli su questa tabella sono ridotti al minimo (deciso insieme a Massimo il 12/09/2012)
                if (entity.Cant_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_CANT_ID));
                if (entity.Cli_Id == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cli_Id),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO,
                      PowerWebResources.FLD_CLI_ID));
                if (string.IsNullOrEmpty(entity.Codice_Cantiere))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_CANTIERE));
                if (entity.FlagGps_Can == Int32.MinValue)
                    if (CommonService.Nz(entity.Descrizione_Can, "") == "")
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Descrizione_Can),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_DESCRIZIONE_CAN));
                if (entity.FlagGps_Can == Int32.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.FlagGps_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_FLAGGPS_CAN));
                if (entity.Tipo_Arrotondamento_Can == Int32.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Tipo_Arrotondamento_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TIPO_ARROTONDAMENTO_CAN));
                if (entity.TipoNotturno_Can == Int32.MinValue)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.TipoNotturno_Can),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_TIPONOTTURNO_CAN));
                //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);
            }
            catch (Exception ex)
            {
                var CodErr = "Cant_Id= " + entity.Cant_Id;
                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Cant_Var entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            entity.FlagGps_Can = 0;
            if (entity.Metodo_Arrotondamento_Can == Int32.MinValue)
                entity.Metodo_Arrotondamento_Can = 0;
            if (entity.Tipo_Arrotondamento_Can == Int32.MinValue)
                entity.Tipo_Arrotondamento_Can = 0;
            if (entity.TipoNotturno_Can == Int32.MinValue)
                entity.TipoNotturno_Can = 0;
            if (entity.Cant_Id == Int32.MinValue)
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cant_Id),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CANT_ID));
            if (String.IsNullOrEmpty(entity.Codice_Cantiere))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cantiere),
                  BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_CANTIERE));
            //verifico SOLO x IMPORT la validità delle eventuali Date ricevute
            if (entity.Data_Registrazione_Can < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Registrazione_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_CAN));
            if (entity.DataOraUltimaModifica_Can < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CAN));
            if (CommonService.Nz(entity.Data_Isee_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Isee_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_ISEE_CAN));
            // commentata perchè in Mosaico hanno anche nati nel 1927!!!!!
            //if (CommonService.Nz(entity.Data_Nascita_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
            //    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Nascita_Can),
                   //BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_NASCITA_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_1_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_1_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_1_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_2_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_2_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_2_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_3_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_3_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_3_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_4_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_4_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_4_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_5_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_5_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_5_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_1_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_1_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_1_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_2_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_2_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_2_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_3_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_3_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_3_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_4_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_4_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_4_CAN));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_5_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_5_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_5_CAN));
            if (CommonService.Nz(entity.DataVarGps_Can, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataVarGps_Can),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAVARGPS_CAN));
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Cant_Var");
            List<PowerMDBDataSet.Cant_VarRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                <PowerMDBDataSet.Cant_VarRow>(oDataSet.Cant_Var.ToList(), "Cant_Var", "RRN").OrderBy(acd => acd.RRN).ToList();
            List<Cant_Var> toImport = new List<Cant_Var>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>(); ;
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.Cant_VarRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_Cantiere.ToString();
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "CANT_VAR",
                                "6", "17", oRow.RRN.ToString(), countRec.ToString(), nRec.ToString()));
                        Cant_Var oNewRecord = this.Init();
                        var a = oRow.RRN;
                        Cant oCant = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere == oRow.Codice_Cantiere);
                        if (oCant != null)
                            oNewRecord.Cant_Id = oCant.Cant_Id;
                        oNewRecord.Codice_Cantiere = oRow.Codice_Cantiere;
                        oNewRecord.Descrizione_Can = oRow.IsDescrizione_CanNull() ? (string)null : oRow.Descrizione_Can;
                        Cli oCli = RepoManager.CliRepo.FirstOrDefault(x => x.Codice_Cliente == oRow.Codice_Cliente_Can);
                        if (oCli != null)
                            oNewRecord.Cli_Id = oCli.Cli_Id;
                        if (!oRow.IsFilale_CanNull())
                        {
                            Fil oRecord = RepoManager.FilRepo.FirstOrDefault(x => x.Codice_Fil == oRow.Filale_Can);
                            if (oRecord != null)
                                oNewRecord.Fil_Id = oRecord.Fil_Id;
                        }

                        oNewRecord.Data_Registrazione_Can = oRow.IsData_Registrazione_CanNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_Can;
                        oNewRecord.DataOraUltimaModifica_Can = oRow.IsDataOraUtimaModificaNull() ? new DateTime(2000, 1, 1) : oRow.DataOraUtimaModifica;
                        oNewRecord.DisAbilitazione_Can = oRow.IsDisAbilitazione_CanNull() ? false : oRow.DisAbilitazione_Can;
                        oNewRecord.Data_Rapporto_Inizio_1_Can = oRow.IsData_Rapporto_Inizio_CanNull() ? (DateTime?)null : oRow.Data_Rapporto_Inizio_Can;
                        oNewRecord.Data_Rapporto_Fine_1_Can = oRow.IsData_Rapporto_Fine_CanNull() ? (DateTime?)null : oRow.Data_Rapporto_Fine_Can;
                        oNewRecord.Luogo_Can = oRow.IsLuogo_CanNull() ? (string)null : oRow.Luogo_Can;
                        oNewRecord.Indirizzo_Can = oRow.IsIndirizzo_CanNull() ? (string)null : oRow.Indirizzo_Can;
                        if (oNewRecord.Indirizzo_Can != null)
                            oNewRecord.Indirizzo_Can = oNewRecord.Indirizzo_Can.Length > 50 ? oNewRecord.Indirizzo_Can.Substring(0, 50) : oNewRecord.Indirizzo_Can;
                        oNewRecord.Provincia_Can = oRow.IsProvincia_CanNull() ? (string)null : oRow.Provincia_Can;
                        oNewRecord.Cap_Can = oRow.IsCap_CanNull() ? (string)null : oRow.Cap_Can;
                        oNewRecord.Nazione_Can = oRow.IsNazione_CanNull() ? (string)null : oRow.Nazione_Can;
                        oNewRecord.Fax_1_Can = oRow.IsFax_1_CanNull() ? (string)null : oRow.Fax_1_Can;
                        oNewRecord.Fax_1_Rif_Can = oNewRecord.Fax_1_Rif_Can;
                        oNewRecord.Fax_2_Can = oRow.IsFax_2_CanNull() ? (string)null : oRow.Fax_2_Can;
                        oNewRecord.Fax_2_Rif_Can = oRow.IsFax_2_Rif_CanNull() ? (string)null : oRow.Fax_2_Rif_Can;
                        oNewRecord.Telefono_1_Can = oRow.IsTelefono_1_CanNull() ? (string)null : oRow.Telefono_1_Can;
                        oNewRecord.Telefono_1_Rif_Can = oRow.IsTelefono_1_Rif_CanNull() ? (string)null : oRow.Telefono_1_Rif_Can;
                        oNewRecord.Telefono_2_Can = oRow.IsTelefono_2_CanNull() ? (string)null : oRow.Telefono_2_Can;
                        oNewRecord.Telefono_2_Rif_Can = oRow.IsTelefono_2_Rif_CanNull() ? (string)null : oRow.Telefono_2_Rif_Can;
                        oNewRecord.Telefono_3_Can = oRow.IsTelefono_3_CanNull() ? (string)null : oRow.Telefono_3_Can;
                        oNewRecord.Telefono_3_Rif_Can = oRow.IsTelefono_3_Rif_CanNull() ? (string)null : oRow.Telefono_3_Rif_Can;
                        oNewRecord.Telefono_4_Can = oRow.IsTelefono_4_CanNull() ? (string)null : oRow.Telefono_4_Can;
                        oNewRecord.Telefono_4_Rif_Can = oRow.IsTelefono_4_Rif_CanNull() ? (string)null : oRow.Telefono_4_Rif_Can;
                        oNewRecord.Mensa_Can = oRow.IsMensa_CanNull() ? false : oRow.Mensa_Can;
                        oNewRecord.Note_Can = oRow.IsNote_CanNull() ? (string)null : oRow.Note_Can;
                        oNewRecord.Minuti_Tolleranza_Can = (short)oRow.Minuti_Tolleranza_Can;
                        oNewRecord.Durata_Min_Ril_Can = oRow.IsDurata_Min_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Min_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Ril_Can = oRow.IsDurata_Max_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Ril_Can = oRow.IsDurata_Max_Gruppo_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Durata_Max_Gruppo_Notte_Ril_Can = oRow.IsDurata_Max_Gruppo_Notte_Ril_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Durata_Max_Gruppo_Notte_Ril_Can.TimeOfDay.Ticks);
                        oNewRecord.Flag_NON_Esportare_Can = oRow.IsFlag_NON_Esportare_CanNull() ? (byte?)null : (byte)oRow.Flag_NON_Esportare_Can;
                        oNewRecord.TipoNotturno_Can = oRow.IsTipoNotturno_CantNull() ? (int)0 : Int32.Parse(oRow.TipoNotturno_Cant);
                        oRow.TipoNotturno_Cant = oRow.IsTipoNotturno_CantNull() ? "0" : oRow.TipoNotturno_Cant;
                        if (!oRow.IsFlagCambioGiornoRil_CanNull() && oRow.FlagCambioGiornoRil_Can == true)
                        {
                            if (!oRow.IsFlagCambioGiornoRil_CanNull() && oRow.TipoNotturno_Cant == "1")
                                oNewRecord.TipoNotturno_Can = 2;
                            else
                                oNewRecord.TipoNotturno_Can = 1;
                        }
                        else
                            oNewRecord.TipoNotturno_Can = 0;
                        oNewRecord.Soglia_Arrot_Fig_Can = oRow.IsSoglia_Arrot_Fig_CanNull() ? (short?)null : (short)oRow.Soglia_Arrot_Fig_Can;
                        oNewRecord.Turno1_Can = oRow.IsTurno1_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno1_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno2_Can = oRow.IsTurno2_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno2_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno3_Can = oRow.IsTurno3_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno3_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno4_Can = oRow.IsTurno4_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno4_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno5_Can = oRow.IsTurno5_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno5_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno6_Can = oRow.IsTurno6_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno6_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno7_Can = oRow.IsTurno7_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno7_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno8_Can = oRow.IsTurno8_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno8_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno9_Can = oRow.IsTurno9_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno9_Can.TimeOfDay.Ticks);
                        oNewRecord.Turno10_Can = oRow.IsTurno10_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Turno10_Can.TimeOfDay.Ticks);
                        oNewRecord.Tipo_Cantiere_Can = oRow.IsTipo_Cantiere_CanNull() ? (string)null : oRow.Tipo_Cantiere_Can;
                        oNewRecord.Singola_Reg = oRow.IsSingola_RegNull() ? false : oRow.Singola_Reg;
                        oNewRecord.Limite_Inizio_Notte_Can = oRow.IsLimite_Inizio_Notte_CanNull() ? (TimeSpan?)null : new TimeSpan(oRow.Limite_Inizio_Notte_Can.TimeOfDay.Ticks);
                        oNewRecord.Soglia_Arrot_Fig_F_Can = oRow.IsSoglia_Arrot_Fig_F_CanNull() ? (short?)null : (short)oRow.Soglia_Arrot_Fig_F_Can;
                        oNewRecord.Minuti_Tolleranza_F_Can = oRow.IsMinuti_Tolleranza_F_CanNull() ? (short?)null : (short)oRow.Minuti_Tolleranza_F_Can;
                        oNewRecord.Arrot_Durata_Can = oRow.IsArrot_Durata_CanNull() ? (short?)null : (short)oRow.Arrot_Durata_Can;
                        oNewRecord.Soglia_Durata_Can = oRow.IsSoglia_Durata_CanNull() ? (short?)null : (short)oRow.Soglia_Durata_Can;
                        oNewRecord.Tipo_Arrotondamento_Can = oRow.IsTipo_Arrotondamento_CanNull() ? (int)0 : (int)oRow.Tipo_Arrotondamento_Can;
                        oNewRecord.Ore_Massime_Can = oRow.IsOre_Massime_CanNull() ? (short?)null : (short)oRow.Ore_Massime_Can;
                        oNewRecord.Tipo_Servizio_1_Can = oRow.IsTipo_Servizio_CanNull() ? (string)null : oRow.Tipo_Servizio_Can;
                        oNewRecord.Data_Nascita_Can = oRow.IsData_Nascita_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Nascita_Can;
                        oNewRecord.Cod_Fisc_Can = oRow.IsCod_Fisc_CanNull() ? (string)null : oRow.Cod_Fisc_Can;
                        oNewRecord.Tipo_Interv_Can = oRow.IsTipo_Interv_CanNull() ? (string)null : oRow.Tipo_Interv_Can;
                        oNewRecord.Metodo_Arrotondamento_Can = oRow.IsMetodo_Arrotondamento_CanNull() ? (int)0 : (int)oRow.Metodo_Arrotondamento_Can;
                        oNewRecord.Percentuale_Can = oRow.IsPercentuale_CanNull() ? (float?)null : (float)oRow.Percentuale_Can;
                        oNewRecord.Costo_Orario_Can = oRow.IsCosto_Orario_CanNull() ? (double?)null : (double)oRow.Costo_Orario_Can;
                        oNewRecord.Residenza_Localita_Can = oRow.IsResidenza_Localita_CanNull() ? (string)null : oRow.Residenza_Localita_Can;
                        oNewRecord.Sesso_Can = oRow.IsSesso_CanNull() ? (string)null : oRow.Sesso_Can;
                        oNewRecord.Residenza_Interno_Can = oRow.IsResidenza_Interno_CanNull() ? (string)null : oRow.Residenza_Interno_Can;
                        oNewRecord.Luogo_Nascita_Can = oRow.IsNascita_Luogo_CanNull() ? (string)null : oRow.Nascita_Luogo_Can;
                        oNewRecord.Provincia_Nascita_Can = oRow.IsNascita_Provincia_CanNull() ? (string)null : oRow.Nascita_Provincia_Can;
                        oNewRecord.Nazione_Nascita_Can = null;
                        oNewRecord.Codice_Luogo_Nascita_Can = oRow.IsCodice_Nascita_Luogo_CanNull() ? (string)null : oRow.Codice_Nascita_Luogo_Can;
                        oNewRecord.Codice_Luogo_Residenza_Can = oRow.IsCodice_Residenza_Luogo_CanNull() ? (string)null : oRow.Codice_Residenza_Luogo_Can;
                        oNewRecord.Nr_Isee_Can = oRow.IsNr_Isee_CanNull() ? (string)null : oRow.Nr_Isee_Can;
                        oNewRecord.Data_Isee_Can = oRow.IsData_Isee_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Isee_Can;
                        oNewRecord.Tipologia_Can = oRow.IsTipologia_CanNull() ? (string)null : oRow.Tipologia_Can;
                        oNewRecord.Gestione_Can = oRow.IsGestione_CanNull() ? (string)null : oRow.Gestione_Can;
                        oNewRecord.Cap_Nascita_Can = oRow.IsCap_Nascita_Luogo_CanNull() ? (string)null : oRow.Cap_Nascita_Luogo_Can;
                        oNewRecord.Canone_Mensile_Fascia1_Can = null;//campo non esistente in Access
                        oNewRecord.Canone_Mensile_Fascia2_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Utenti_Fascia1_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Utenti_Fascia2_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Bimbi_Fascia_Can = null;//campo non esistente in Access
                        oNewRecord.Costo_Mezzora_Prescuola_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Utenti_PreScuola_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Utenti_PostScuola_1Mezzora_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_Utenti_PostScuola_2Mezzora_Can = null;//campo non esistente in Access
                        oNewRecord.Contributo_Disabili_Fascia1_Can = null;//campo non esistente in Access
                        oNewRecord.Contributo_Disabili_Fascia2_Can = null;//campo non esistente in Access
                        oNewRecord.Numero_GG_Lavorativi = oRow.IsNumero_GG_LavorativiNull() ? (short?)null : (short)oRow.Numero_GG_Lavorativi;
                        oNewRecord.Tipo_Servizio_2_Can = oRow.IsTipo_Servizio_2_CanNull() ? (string)null : oRow.Tipo_Servizio_2_Can;
                        oNewRecord.Tipo_Servizio_3_Can = oRow.IsTipo_Servizio_3_CanNull() ? (string)null : oRow.Tipo_Servizio_3_Can;
                        oNewRecord.Tipo_Servizio_4_Can = oRow.IsTipo_Servizio_4_CanNull() ? (string)null : oRow.Tipo_Servizio_4_Can;
                        oNewRecord.Tipo_Servizio_5_Can = oRow.IsTipo_Servizio_5_CanNull() ? (string)null : oRow.Tipo_Servizio_5_Can;
                        oNewRecord.Data_Rapporto_Inizio_2_Can = oRow.IsData_Rapporto_Inizio_2_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Inizio_2_Can;
                        oNewRecord.Data_Rapporto_Fine_2_Can = oRow.IsData_Rapporto_Fine_2_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Fine_2_Can;
                        oNewRecord.Data_Rapporto_Inizio_3_Can = oRow.IsData_Rapporto_Inizio_3_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Inizio_3_Can;
                        oNewRecord.Data_Rapporto_Fine_3_Can = oRow.IsData_Rapporto_Fine_3_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Fine_3_Can;
                        oNewRecord.Data_Rapporto_Inizio_4_Can = oRow.IsData_Rapporto_Inizio_4_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Inizio_4_Can;
                        oNewRecord.Data_Rapporto_Fine_4_Can = oRow.IsData_Rapporto_Fine_4_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Fine_4_Can;
                        oNewRecord.Data_Rapporto_Inizio_5_Can = oRow.IsData_Rapporto_Inizio_5_CanNull() ? (DateTime?)null : (DateTime)oRow.Data_Rapporto_Inizio_5_Can;
                        oNewRecord.Data_Rapporto_Fine_5_Can = null;//campo non esistente in Access
                        oNewRecord.Percentuale_Servizio_1_Can = oRow.IsPercentuale_Servizio_CanNull() ? (float?)null : (float)oRow.Percentuale_Servizio_Can;
                        oNewRecord.Percentuale_Servizio_2_Can = oRow.IsPercentuale_Servizio_2_CanNull() ? (float?)null : (float)oRow.Percentuale_Servizio_2_Can;
                        oNewRecord.Percentuale_Servizio_3_Can = oRow.IsPercentuale_Servizio_3_CanNull() ? (float?)null : (float)oRow.Percentuale_Servizio_3_Can;
                        oNewRecord.Percentuale_Servizio_4_Can = oRow.IsPercentuale_Servizio_4_CanNull() ? (float?)null : (float)oRow.Percentuale_Servizio_4_Can;
                        oNewRecord.Percentuale_Servizio_5_Can = oRow.IsPercentuale_Servizio_5_CanNull() ? (float?)null : (float)oRow.Percentuale_Servizio_5_Can;
                        oNewRecord.Codice_Voucher_Can = oRow.IsCodice_Vaucher_CanNull() ? (string)null : oRow.Codice_Vaucher_Can;
                        oNewRecord.Livello_Assistito_Can = oRow.IsLivello_Assistito_CanNull() ? (string)null : oRow.Livello_Assistito_Can;
                        oNewRecord.Tipo_Calcolo_Viaggi_Can = oRow.IsTipo_Calcolo_Viaggi_CanNull() ? (string)null : oRow.Tipo_Calcolo_Viaggi_Can;
                        oNewRecord.DataVarGps_Can = oRow.IsDataVarGps_CanNull() ? (DateTime?)null : (DateTime)oRow.DataVarGps_Can;
                        oNewRecord.LatitudineGps_Can = oRow.IsLatitudineGps_CanNull() ? (double?)null : (double)oRow.LatitudineGps_Can;
                        oNewRecord.LongitudineGps_Can = oRow.IsLongitudineGps_CanNull() ? (double?)null : (double)oRow.LongitudineGps_Can;
                        oNewRecord.RaggioGps_Can = oRow.IsRaggioGps_CanNull() ? (short?)null : (short)oRow.RaggioGps_Can;
                        oNewRecord.Raggruppamento1_Can = "";
                        oNewRecord.Raggruppamento2_Can = "";
                        oNewRecord.Cognome_Assistito_Can = oNewRecord.Descrizione_Can;
                        oNewRecord.Nome_Assistito_Can = "";
                        oNewRecord.Limite_Entrata_Mattina_Cant = oRow.IsLimite_Entrata_canNull() ? (TimeSpan?)null : oRow.Limite_Entrata_can.TimeOfDay;
                        //// Eseguo i Controlli Specifici della CheckForImport e poi i Controlli Standard della Check        
                        Dictionary<string, string> importDictionary = CheckForImport(oNewRecord);
                        //Solo la prima volta chiamo la Check con ResetSession=True per fargli aggironare i Dati dal DB
                        if (countRec == 1)
                            currentDictionary = Check(oNewRecord, true, true);
                        else
                            currentDictionary = Check(oNewRecord, true, false);

                        if (currentDictionary.Keys.Count == 0 && importDictionary.Keys.Count == 0)
                            toImport.Add(oNewRecord);
                        else
                        {
                            errors.Add(new Tab_Chk_Imp
                            {
                                Nome_Tabella_Tab_Check_Imp = "Cant_Var",
                                Chiave_Record_Tab_Check_Imp = oRow.RRN.ToString(),
                            });
                            log.Warn("Codice Cantiere cui si rifericono gli errori precedenti: " + oRow.Codice_Cantiere + " - RRN : " + oRow.RRN + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                        }
                    }
                    catch (Exception ex)
                    {
                        var RRNERR = oRow.RRN;
                        //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                        if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                            Log.ErrorFormat("TAB CANT_VAR - Chiave: {0}", oRow.RRN.ToString());
                        throw ex;
                    }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                //try
                //{
                BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                                      BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "CANT_VAR", lastKey));
                RepoManager.Cant_VarRepo.Add(toImport, true);
                RepoManager.Tab_Chk_ImpRepo.Add(errors);
                if (errors.Count == 0)
                    RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                    {
                        Nome_Tabella_Tab_Check_Imp = "Cant_Var",
                        Stato_Record_Tab_Check_Imp = true,
                    });
                RepoManager.Tab_Chk_ImpRepo.SaveChanges();
                RepoManager.Tab_Chk_ImpRepo.CommitWork();
                ResetSession();
                //}
                //catch (Exception ex)
                //{
                //    RepoManager.Tab_Chk_ImpRepo.RollbackWork();
                //    ResetSession();
                //    throw ex;
                //}
            }
            return errorsList;
        }

        public override Expression<Func<Cant_Var, bool>> Filter
        {
            get
            {
                if ((PowerWebContext.Current.DomainFilter & DomainFilterEnum.Fil) == DomainFilterEnum.Fil && PowerWebContext.Current.Fils != null && PowerWebContext.Current.User.Liv_Utente < 10)
                {
                    var allCantIds = PowerWebContext.Current.CantsIds;
                    return cantVar => allCantIds.Contains(cantVar.Cant_Id);
                }
                else return base.Filter;
            }
        }
    }
}
