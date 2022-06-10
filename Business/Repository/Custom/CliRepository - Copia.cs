using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Data;
using Business.MDBSchema;
using Common;
using System.Text;
using log4net;

namespace Business.Repository.Custom
{
    public class Cli_VRepository : GenericRepository<Cli>, ICliRepository
    {
        public Cli_VRepository(PowerWebEntities context)
            : base(context)
        {
        }

        //Serve per la Verifica che nel frattempo nessun altro Utente abbia modificato il Record 
        private static DateTime DataOraRecord;

        /// <summary>Ottiene le tabelle che deve usare in formato lista.
        /// Per ottimizzare la velocità salva l'oggetto nella sessione corrente
        /// in modo che la lista sia già in memoria quando viene richiesta più volte.
        /// </summary>
        private static List<Fil> Fils
        {
            get
            {
                List<Fil> oLista = PowerWebContext.GetFromSession<List<Fil>>("Fils_CliRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.FilRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Fil>>("Fils_CliRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Comuni> Tab_Comunis
        {
            get
            {
                List<Tab_Comuni> oLista = PowerWebContext.GetFromSession<List<Tab_Comuni>>("Tab_Comunis_CliRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ComuniRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Comuni>>("Tab_Comunis_CliRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Prov> Tab_Provs
        {
            get
            {
                List<Tab_Prov> oLista = PowerWebContext.GetFromSession<List<Tab_Prov>>("Tab_Provs_CliRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_ProvRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_CliRepo", oLista);
                }
                return oLista;
            }
        }
        private static List<Tab_Decod> Tab_Decods
        {
            get
            {
                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_CliRepo");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_CliRepo", oLista);
                }
                return oLista;
            }
        }

        private static void ResetSession()
        {
            PowerWebContext.SetToSession<List<Fil>>("Fils_CliRepo", null);
            PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Provs_CliRepo", null);
            PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Comunis_CliRepo", null);
            PowerWebContext.SetToSession<List<Tab_Prov>>("Tab_Decods_CliRepo", null);
        }

        public override Cli Init()
        {
            Cli oNewRecord = base.Init();
            oNewRecord.DisAbilitazione_Cli = false;
            oNewRecord.Data_Registrazione_Cli = DateTime.UtcNow;
            oNewRecord.DataOraUltimaModifica_Cli = DateTime.UtcNow;
            return oNewRecord;
        }

        public override void SetEntityBeforeAddOrUpdate(Cli entity)
        {
             //Salvo la DataOraUltimaModifica di quando era stato letto il Record dal Db x verificare che nessuno lo abbia modificato nel frattempo
            DataOraRecord = entity.DataOraUltimaModifica_Cli;
            entity.DataOraUltimaModifica_Cli = DateTime.UtcNow;
            entity.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Cliente, 20);
           
        }

        public override Dictionary<string, string> Check(Cli entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
             //Serve x Rileggere i Dati ATTUALI dal DB per fare i controlli allineati alle ultima Modifiche fatte sul DB
            if (isResetSession)
                ResetSession();

            try
            {
                //Leggo la DataOraUltimaModifica ATTUALE dal REcord del DB per verificare che nessuno abbia modificato il Record nel frattempo                
                if (!isNew)
                {
                    DateTime DataOraRecordDb = RepoManager.CliRepo.Single(u => u.Cli_Id == entity.Cli_Id).DataOraUltimaModifica_Cli;
                    if (DataOraRecordDb > DataOraRecord)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_MODIFICATO_NEL_FRATTEMPO_DA_ALTRO_UTENTE, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CLI));
                    }
                }
                //      
                //1) verifico che il Valore della Chiave sia impostato perché è obbligatorio e che sia univoco
                //
                if (String.IsNullOrEmpty(entity.Codice_Cliente))
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cliente),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_CODICE_UTENTE));
                else
                {
                    entity.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(entity.Codice_Cliente, 20);
                    if (isNew)
                    {
                        if (RepoManager.CliRepo.SingleOrDefault(u => u.Codice_Cliente == entity.Codice_Cliente) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cliente),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                    else
                    {
                        if (RepoManager.CliRepo.SingleOrDefault(u => u.Codice_Cliente == entity.Codice_Cliente && u.Cli_Id != entity.Cli_Id) != null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cliente),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_RECORD_CON_VALORI_DUPLICATI));
                    }
                }
                //
                //2) verifico i campi obbligatori e che siano eventualmente presenti nella relativa Tabella
                //
                if (CommonService.Nz(entity.Cognome_Cli, "") == "")
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_E_OBBLIGATORIO, PowerWebResources.FLD_COGNOME_ASSISTITO_CAN));
                //
                //3) verifico, per una serie di campi, che il valore di un campo sia minore del valore di un altro campo
                //
                if (CommonService.Nz(entity.Data_Rapporto_Inizio_Cli, new DateTime(1, 1, 1))
                > CommonService.Nz(entity.Data_Rapporto_Fine_Cli, new DateTime(9999, 1, 1)))
                    result.AddOrAppend(new StringBuilder(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_Cli)).Append("-")
                      .Append(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_Cli)).ToString(),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_INIZIO_X_DEVE_ESSERE_MINORE_DI_DATA_FINE_Y,
                      PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_CLI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_CLI));
                //
                //4) verifico, per una serie di campi, che il valore del campo sia corretto
                //
                if (CommonService.Nz(entity.Banca_Iban_Cli, "") != "")
                    if (!CommonService.EIbanValido(entity.Banca_Iban_Cli))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Banca_Iban_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.STR_VALORE_CAMPO_X_ERRATO,
                        PowerWebResources.FLD_BANCA_IBAN_CLI));
                if (CommonService.Nz(entity.Codice_Fiscale_Cli, "") != "")
                    if (!CommonService.ECodiceFiscaleValido(entity.Codice_Fiscale_Cli))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_CODICE_FISCALE_ERRATO));
                if (CommonService.Nz(entity.Pagamento_1Mese_Escl_Cli, 0) > 12)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_1Mese_Escl_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_PAGAMENTO_1MESE_ESCL_CLI, PowerWebResources.VALORE_12));
                if (CommonService.Nz(entity.Pagamento_2Mese_Escl_Cli, 0) > 12)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_2Mese_Escl_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_PAGAMENTO_2MESE_ESCL_CLI, PowerWebResources.VALORE_12));
                if (CommonService.Nz(entity.Pagamento_GG_Cli, 0) > 31)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_GG_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_PAGAMENTO_GG_CLI, PowerWebResources.VALORE_31));
                if (CommonService.Nz(entity.Pagamento_Intervallo_Cli, 0) > 240)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_Intervallo_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_PAGAMENTO_INTERVALLO_CLI, PowerWebResources.VALORE_240));
                if (CommonService.Nz(entity.Pagamento_Rate_Cli, 0) > 10)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_Rate_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                      PowerWebResources.FLD_PAGAMENTO_RATE_CLI, PowerWebResources.VALORE_10));
                if (new string[] { "0", "2", "FM" }.Where(x => x == CommonService.Nz(entity.Pagamento_GF_Cli, "0")).Count() == 0)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_GF_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_DEVE_AVERE_UNO_DEI_SEGUENTI_VALORI_Y,
                      PowerWebResources.FLD_PAGAMENTO_GF_CLI, PowerWebResources.VALORE_0_2_FM));
                if (CommonService.Nz(entity.Banca_Iban_Cli, "") != "")
                    if (!CommonService.EIbanValido(entity.Banca_Iban_Cli))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Banca_Iban_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.STR_VALORE_CAMPO_X_ERRATO, PowerWebResources.FLD_BANCA_IBAN_CLI));
                if (CommonService.Nz(entity.Partita_Iva_Cli, "") != "")
                    if (!CommonService.EPartitaIvaValida(entity.Partita_Iva_Cli))
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partita_Iva_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.STR_VALORE_CAMPO_X_ERRATO,
                        PowerWebResources.FLD_PARTITA_IVA_CLI));
                //
                //4.1) verifico, per una serie di campi, che la lunghezza delle stringhe sia corretta con il valore nel DB
                //
                if (CommonService.Nz(entity.Codice_Cliente, "") != "")
                    if (entity.Codice_Cliente.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Cliente),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_CLIENTE, PowerWebResources.VALORE_20 ));
                if (CommonService.Nz(entity.Banca_Descrizione_Cli, "") != "")
                    if (entity.Banca_Descrizione_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Banca_Descrizione_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_BANCA_DESCRIZIONE_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Banca_Iban_Cli, "") != "")
                    if (entity.Banca_Iban_Cli.Length > 40)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Banca_Iban_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_BANCA_IBAN_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Codice_Ditta_Cli, "") != "")
                    if (entity.Codice_Ditta_Cli.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Ditta_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_DITTA_CLI, PowerWebResources.VALORE_20 ));
                if (CommonService.Nz(entity.Codice_Fiscale_Cli, "") != "")
                    if (entity.Codice_Fiscale_Cli.Length > 16)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Codice_Fiscale_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_CODICE_FISCALE_CLI, PowerWebResources.VALORE_16 ));
                if (CommonService.Nz(entity.Cognome_Cli, "") != "")
                    if (entity.Cognome_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Cognome_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_COGNOME_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Domicilio_Cap_Cli, "") != "")
                    if (entity.Domicilio_Cap_Cli.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_CAP_CLI, PowerWebResources.VALORE_15 ));
                if (CommonService.Nz(entity.Domicilio_Indirizzo_Cli, "") != "")
                    if (entity.Domicilio_Indirizzo_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Indirizzo_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_INDIRIZZO_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Domicilio_Luogo_Cli, "") != "")
                    if (entity.Domicilio_Luogo_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Luogo_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_LUOGO_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Domicilio_Provincia_Cli, "") != "")
                    if (entity.Domicilio_Provincia_Cli.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Provincia_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_PROVINCIA_CLI, PowerWebResources.VALORE_4 ));
                if (CommonService.Nz(entity.Domicilio_Nazione_Cli, "") != "")
                    if (entity.Domicilio_Nazione_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Nazione_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_DOMICILIO_NAZIONE_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Fax_1_Cli, "") != "")
                    if (entity.Fax_1_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Fax_1_Rif_Cli, "") != "")
                    if (entity.Fax_1_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_1_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_1_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Fax_2_Cli, "") != "")
                    if (entity.Fax_2_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_2_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_2_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Fax_2_Rif_Cli, "") != "")
                    if (entity.Fax_2_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Fax_2_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_FAX_2_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Lingua_Cli, "") != "")
                    if (entity.Lingua_Cli.Length > 5)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Lingua_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_LINGUA_CLI, PowerWebResources.VALORE_5 ));
                if (CommonService.Nz(entity.Nome_Cli, "") != "")
                    if (entity.Nome_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Nome_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_NOME_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Pagamento_Codice_Cli, "") != "")
                    if (entity.Pagamento_Codice_Cli.Length > 10)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_Codice_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PAGAMENTO_CODICE_CLI, PowerWebResources.VALORE_10 ));
                if (CommonService.Nz(entity.Pagamento_GF_Cli, "") != "")
                    if (entity.Pagamento_GF_Cli.Length > 2)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_GF_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PAGAMENTO_GF_CLI, PowerWebResources.VALORE_2 ));
                if (CommonService.Nz(entity.Partita_Iva_Cli, "") != "")
                    if (entity.Partita_Iva_Cli.Length > 20)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Partita_Iva_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_PARTITA_IVA_CLI, PowerWebResources.VALORE_20 ));
                if (CommonService.Nz(entity.Residenza_Cap_Cli, "") != "")
                    if (entity.Residenza_Cap_Cli.Length > 15)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_CAP_CLI, PowerWebResources.VALORE_15 ));
                if (CommonService.Nz(entity.Residenza_Indirizzo_Cli, "") != "")
                    if (entity.Residenza_Indirizzo_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Indirizzo_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_INDIRIZZO_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Residenza_Luogo_Cli, "") != "")
                    if (entity.Residenza_Luogo_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_LUOGO_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Residenza_Nazione_Cli, "") != "")
                    if (entity.Residenza_Nazione_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Nazione_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_NAZIONE_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Residenza_Provincia_Cli, "") != "")
                    if (entity.Residenza_Provincia_Cli.Length > 4)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_RESIDENZA_PROVINCIA_CLI, PowerWebResources.VALORE_4 ));
                if (CommonService.Nz(entity.Telefono_1_Cli, "") != "")
                    if (entity.Telefono_1_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_1_Rif_Cli, "") != "")
                    if (entity.Telefono_1_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_1_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_1_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_2_Cli, "") != "")
                    if (entity.Telefono_2_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_2_Rif_Cli, "") != "")
                    if (entity.Telefono_2_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_2_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_2_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_3_Cli, "") != "")
                    if (entity.Telefono_3_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_3_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_3_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_3_Rif_Cli, "") != "")
                    if (entity.Telefono_3_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_3_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_3_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_4_Cli, "") != "")
                    if (entity.Telefono_4_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_4_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_4_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Telefono_4_Rif_Cli, "") != "")
                    if (entity.Telefono_4_Rif_Cli.Length > 50)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Telefono_4_Rif_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_TELEFONO_4_RIF_CLI, PowerWebResources.VALORE_50 ));
                if (CommonService.Nz(entity.Valuta_Cli, "") != "")
                    if (entity.Valuta_Cli.Length > 5)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Valuta_Cli),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                        PowerWebResources.FLD_VALUTA_CLI, PowerWebResources.VALORE_5));
            
                //5) verifico, per una serie di campi, che il valore del campo sia presente nella relativa Tabella
                //            
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Domicilio_Cap_Cli, "") != "")
                        if (Tab_Comunis.FirstOrDefault(u => u.Cap_Tab_Comuni == entity.Domicilio_Cap_Cli) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Cli),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                                PowerWebResources.FLD_DOMICILIO_CAP_CLI, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Domicilio_Luogo_Cli, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni == entity.Domicilio_Cap_Cli) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Cli),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_DOMICILIO_LUOGO_CLI, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Domicilio_Cap_Cli, "") != "")
                        if (entity.Domicilio_Cap_Cli.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Cap_Cli),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_DOMICILIO_CAP_CLI, PowerWebResources.VALORE_15 ));
                    if (CommonService.Nz(entity.Domicilio_Luogo_Cli, "") != "")
                        if (entity.Domicilio_Luogo_Cli.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Luogo_Cli),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_DOMICILIO_LUOGO_CLI, PowerWebResources.VALORE_50 ));
                }
                if (CommonService.Nz(entity.Domicilio_Nazione_Cli, "") != "")
                    if (RepoManager.Tab_DecodRepo.SearchKeyInTable(TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                    TabDecodNameEnum.SIGLA_NAZIONI.ToString(), entity.Domicilio_Nazione_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Nazione_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_DOMICILIO_NAZIONE_CLI));
                if (CommonService.Nz(entity.Domicilio_Provincia_Cli, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Domicilio_Provincia_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Domicilio_Provincia_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                            PowerWebResources.FLD_DOMICILIO_PROVINCIA_CLI, PowerWebResources.STR_TAB_PROV));
                if (CommonService.Nz(entity.Lingua_Cli, "") != "")
                  if (RepoManager.Tab_DecodRepo.SearchKeyInTable(TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                  TabDecodNameEnum.LINGUA_CLI.ToString (), entity.Lingua_Cli ) == null)
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Lingua_Cli),
                      BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                      PowerWebResources.FLD_LINGUA_CLI));
                if (CommonService.Nz(entity.Pagamento_Codice_Cli, "") != "")
                    if (RepoManager.Tab_DecodRepo.SearchKeyInTable(TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                    TabDecodNameEnum.TIPO_PAGAMENTO.ToString(), entity.Pagamento_Codice_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Pagamento_Codice_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_PAGAMENTO_CODICE_CLI));
                if (RepoManager.ParamRepo.ParametersRow.Ctrl_Tab_Comuni != 0)
                // I Controlli sui CAP/LUOGHI/CODICI LUOGHI sono FATTI SOLO se è alzato il Falg CTRL_TAB_COMUNI in PARAM
                {
                    if (CommonService.Nz(entity.Residenza_Cap_Cli, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Cap_Tab_Comuni == entity.Residenza_Cap_Cli) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Cli),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_RESIDENZA_CAP_CLI, PowerWebResources.STR_TAB_COMUNI));
                    if (CommonService.Nz(entity.Residenza_Luogo_Cli, "") != "")
                        if (Tab_Comunis.FirstOrDefault(x => x.Luogo_Tab_Comuni == entity.Residenza_Luogo_Cli) == null)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Cli),
                              BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_Y,
                              PowerWebResources.FLD_RESIDENZA_LUOGO_CLI, PowerWebResources.STR_TAB_COMUNI));
                }
                else
                // Altrimenti mi limito a controllarne solo la Lunghezza massima    
                {
                    if (CommonService.Nz(entity.Residenza_Cap_Cli, "") != "")
                        if (entity.Residenza_Cap_Cli.Length > 15)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Cap_Cli),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_RESIDENZA_CAP_CLI, PowerWebResources.VALORE_15 ));
                    if (CommonService.Nz(entity.Residenza_Luogo_Cli, "") != "")
                        if (entity.Residenza_Luogo_Cli.Length > 50)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Luogo_Cli),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_LUNGHEZZA_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                            PowerWebResources.FLD_RESIDENZA_LUOGO_CLI, PowerWebResources.VALORE_50 ));
                }
                if (CommonService.Nz(entity.Residenza_Nazione_Cli, "") != "")
                    if (RepoManager.Tab_DecodRepo.SearchKeyInTable(TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                    TabDecodNameEnum.SIGLA_NAZIONI.ToString(), entity.Residenza_Nazione_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Nazione_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_RESIDENZA_NAZIONE_CLI));
                if (CommonService.Nz(entity.Residenza_Provincia_Cli, "") != "")
                    if (Tab_Provs.SingleOrDefault(u => u.Sigla_Prov == entity.Residenza_Provincia_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Residenza_Provincia_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                            PowerWebResources.FLD_RESIDENZA_PROVINCIA_CLI));
                if (CommonService.Nz(entity.Valuta_Cli, "") != "")
                    if (RepoManager.Tab_DecodRepo.SearchKeyInTable(TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                    TabDecodNameEnum.VALUTE.ToString(), entity.Valuta_Cli) == null)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Valuta_Cli),
                          BusinessService.GetLocalizedString(PowerWebResources.ERR_VALORE_CAMPO_X_NON_PRESENTE_IN_TABELLA_DECODIFICHE,
                          PowerWebResources.FLD_VALUTA_CLI));
             //
                //6) Scrittura del Record di LOG
                //
                WriteCheckLog(entity, result, Log);

            }
            catch (Exception ex)
            {
                var strError = String.Format("Errore CliId: {0} - {1}", entity.Cli_Id, ex.Message);
                result.AddOrAppend(BusinessService.GetLocalizedString(PowerWebResources.STR_ERRORE), strError);

                WriteCheckLog(entity, result, Log);

                throw ex;
            }
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Cli entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //verifico SOLO x IMPORT la validità delle eventuali Date ricevute
            if (entity.Data_Registrazione_Cli < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Registrazione_Cli),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_REGISTRAZIONE_CLI));
            if (entity.DataOraUltimaModifica_Cli < new DateTime(2000, 01, 01))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.DataOraUltimaModifica_Cli),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATAORAULTIMAMODIFICA_CLI));
            if (CommonService.Nz(entity.Data_Rapporto_Inizio_Cli, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Inizio_Cli),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_INIZIO_CLI));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_Cli, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_Cli),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_CLI));
            if (CommonService.Nz(entity.Data_Rapporto_Fine_Cli, new DateTime(2002, 1, 1)) < new DateTime(2001, 1, 1))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Rapporto_Fine_Cli),
                   BusinessService.GetLocalizedString(PowerWebResources.ERR_CAMPO_X_CONTIENE_VALORI_NON_VALIDI, PowerWebResources.FLD_DATA_RAPPORTO_FINE_CLI));
            
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override List<Dictionary<String, String>> ImportFromDataSet(PowerMDBDataSet oDataSet, bool onlyErrors = false)
        {
            var errorsList = new List<Dictionary<String, String>>();
            ILog log = LogManager.GetLogger("Cli");
            Dictionary<string, string> oResultDictionary = new Dictionary<string, string>();
            List<PowerMDBDataSet.CliRow> accessData = RepoManager.Tab_Chk_ImpRepo.GetImportErrorData
                    <PowerMDBDataSet.CliRow>(oDataSet.Cli.ToList(), "Cli", "Codice_Cliente").OrderBy(acd => acd.Codice_Cliente).ToList(); ;
            List<Cli> toImport = new List<Cli>();
            List<Tab_Chk_Imp> errors = new List<Tab_Chk_Imp>();
            Dictionary<string, string> currentDictionary = new Dictionary<string, string>();
            string lastKey = "";
            if (accessData != null)
            {
                double nRec = accessData.Count;
                double countRec = 0;
                double percRec = 0;
                foreach (PowerMDBDataSet.CliRow oRow in accessData)
                {
                    try
                    {
                        lastKey = oRow.Codice_Cliente;
                        countRec = countRec + 1;
                        percRec = (countRec / nRec) * 100;                        
                        BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                            BusinessService.GetLocalizedString(PowerWebResources.STR_STO_IMPORTANDO_TAB_X_DI_Y_CHIAVE_COUNT_DI.ToString(), "CLI",
                            "4", "17", oRow.Codice_Cliente.ToString(), countRec.ToString(), nRec.ToString()));
                        Cli oNewRecord = this.Init();
                        oNewRecord.Codice_Cliente = oRow.Codice_Cliente;
                        oNewRecord.Codice_Ditta_Cli = oRow.IsCodice_Ditta_CliNull() ? (string)null : oRow.Codice_Ditta_Cli;
                        if (!oRow.IsCognome_CliNull() || !oRow.IsNome_CliNull())
                        {
                            oNewRecord.Cognome_Cli = oRow.IsCognome_CliNull() ? (string)"" : oRow.Cognome_Cli;
                            oNewRecord.Nome_Cli = oRow.IsNome_CliNull() ? (string)"" : oRow.Nome_Cli;
                        }
                        oNewRecord.Data_Registrazione_Cli = oRow.IsData_Registrazione_CliNull() ? new DateTime(2000, 1, 1) : oRow.Data_Registrazione_Cli;
                        oNewRecord.DataOraUltimaModifica_Cli = oRow.IsDataOraUtimaModificaNull() ? new DateTime(2000, 1, 1) : oRow.DataOraUtimaModifica;
                        oNewRecord.DisAbilitazione_Cli = oRow.IsDisAbilitazione_CliNull() ? false : oRow.DisAbilitazione_Cli;
                        oNewRecord.Data_Rapporto_Inizio_Cli = oRow.IsData_Rapporto_Inizio_CliNull() ? (DateTime?)null : oRow.Data_Rapporto_Inizio_Cli;
                        oNewRecord.Data_Rapporto_Fine_Cli = oRow.IsData_Rapporto_Fine_CliNull() ? (DateTime?)null : oRow.Data_Rapporto_Fine_Cli;
                        oNewRecord.Residenza_Luogo_Cli = oRow.IsResidenza_Luogo_CliNull() ? (string)null : oRow.Residenza_Luogo_Cli;
                        oNewRecord.Residenza_Indirizzo_Cli = oRow.IsResidenza_Indirizzo_CliNull() ? (string)null : oRow.Residenza_Indirizzo_Cli;
                        oNewRecord.Residenza_Provincia_Cli = oRow.IsResidenza_Provincia_CliNull() ? (string)null : oRow.Residenza_Provincia_Cli;
                        oNewRecord.Residenza_Cap_Cli = oRow.IsResidenza_Cap_CliNull() ? (string)null : oRow.Residenza_Cap_Cli;
                        oNewRecord.Residenza_Nazione_Cli = oRow.IsResidenza_Nazione_CliNull() ? (string)null : oRow.Residenza_Nazione_Cli;
                        oNewRecord.Domicilio_Luogo_Cli = oRow.IsDomicilio_Luogo_CliNull() ? (string)null : oRow.Domicilio_Luogo_Cli;
                        oNewRecord.Domicilio_Indirizzo_Cli = oRow.IsDomicilio_Indirizzo_CliNull() ? (string)null : oRow.Domicilio_Indirizzo_Cli;
                        oNewRecord.Domicilio_Provincia_Cli = oRow.IsDomicilio_Provincia_CliNull() ? (string)null : oRow.Domicilio_Provincia_Cli;
                        oNewRecord.Domicilio_Cap_Cli = oRow.IsDomicilio_Cap_CliNull() ? (string)null : oRow.Domicilio_Cap_Cli;
                        oNewRecord.Domicilio_Nazione_Cli = oRow.Is_Domicilio_Nazione_CliNull() ? (string)null : oRow._Domicilio_Nazione_Cli;
                        oNewRecord.Fax_1_Cli = oRow.IsFax_1_CliNull() ? (string)null : oRow.Fax_1_Cli;
                        oNewRecord.Fax_1_Rif_Cli = oRow.IsFax_1_Rif_CliNull() ? (string)null : oRow.Fax_1_Rif_Cli;
                        oNewRecord.Fax_2_Cli = oRow.IsFax_2_CliNull() ? (string)null : oRow.Fax_2_Cli;
                        oNewRecord.Fax_2_Rif_Cli = oRow.IsFax_2_Rif_CliNull() ? (string)null : oRow.Fax_2_Rif_Cli;
                        oNewRecord.Telefono_1_Cli = oRow.IsTelefono_1_CliNull() ? (string)null : oRow.Telefono_1_Cli;
                        oNewRecord.Telefono_1_Rif_Cli = oRow.IsTelefono_1_Rif_CliNull() ? (string)null : oRow.Telefono_1_Rif_Cli;
                        oNewRecord.Telefono_2_Cli = oRow.IsTelefono_2_CliNull() ? (string)null : oRow.Telefono_2_Cli;
                        oNewRecord.Telefono_2_Rif_Cli = oRow.IsTelefono_2_Rif_CLiNull() ? (string)null : oRow.Telefono_2_Rif_CLi;
                        oNewRecord.Telefono_3_Cli = oRow.IsTelefono_3_CliNull() ? (string)null : oRow.Telefono_3_Cli;
                        oNewRecord.Telefono_3_Rif_Cli = oRow.IsTelefono_3_Rif_CliNull() ? (string)null : oRow.Telefono_3_Rif_Cli;
                        oNewRecord.Telefono_4_Cli = oRow.IsTelefono_4_CliNull() ? (string)null : oRow.Telefono_4_Cli;
                        oNewRecord.Telefono_4_Rif_Cli = oRow.IsTelefono_4_Rif_CliNull() ? (string)null : oRow.Telefono_4_Rif_Cli;
                        oNewRecord.Partita_Iva_Cli = oRow.IsPartita_Iva_CliNull() ? (string)null : oRow.Partita_Iva_Cli;
                        oNewRecord.Codice_Fiscale_Cli = oRow.IsCodice_Fiscale_CliNull() ? (string)null : oRow.Codice_Fiscale_Cli;
                        if (!oRow.IsBanca_Abi_CliNull() || !oRow.IsBanca_Cab_CliNull())
                        {
                            oNewRecord.Banca_Iban_Cli = String.Format("ABI+CAB:{0} {1}",
                              oRow.IsBanca_Abi_CliNull() ? (string)null : oRow.Banca_Abi_Cli,
                              oRow.IsBanca_Cab_CliNull() ? (string)null : oRow.Banca_Cab_Cli);
                        }
                        oNewRecord.Banca_Descrizione_Cli = oRow.IsBanca_Descrizione_CliNull() ? (string)null : oRow.Banca_Descrizione_Cli;
                        oNewRecord.Pagamento_Codice_Cli = oRow.IsPagamento_Codice_CliNull() ? (string)null : oRow.Pagamento_Codice_Cli;
                        oNewRecord.Pagamento_Rate_Cli = oRow.IsPagamento_Rate_CliNull() ? (byte?)null : (byte)oRow.Pagamento_Rate_Cli;
                        oNewRecord.Pagamento_GG_Cli = oRow.IsPagamento_GG_CliNull() ? (byte?)null : (byte)oRow.Pagamento_GG_Cli;
                        oNewRecord.Pagamento_Sconto_Cli = oRow.IsPagamento_Sconto_CliNull() ? (byte?)null : (byte)oRow.Pagamento_Sconto_Cli;
                        oNewRecord.Pagamento_Intervallo_Cli = oRow.IsPagamento_Intervallo_CliNull() ? (byte?)null : (byte)oRow.Pagamento_Intervallo_Cli;
                        oNewRecord.Note_Cli = oRow.IsNote_CliNull() ? (string)null : oRow.Note_Cli;
                        oNewRecord.Pagamento_GF_Cli = oRow.IsPagamento_GF_CliNull() ? (string)null : oRow.Pagamento_GF_Cli;
                        oNewRecord.Pagamento_1Mese_Escl_Cli = oRow.IsPagamento_1Mese_Escl_CliNull() ? (byte?)null : (byte)oRow.Pagamento_1Mese_Escl_Cli;
                        oNewRecord.Pagamento_2Mese_Escl_Cli = oRow.IsPagamento_2Mese_Escl_CliNull() ? (byte?)null : (byte)oRow.Pagamento_2Mese_Escl_Cli;
                        oNewRecord.Valuta_Cli = oRow.IsValuta_CliNull() ? (string)null : oRow.Valuta_Cli;
                        oNewRecord.Lingua_Cli = oRow.IsLingua_CliNull() ? (string)null : oRow.Lingua_Cli;

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
                                Nome_Tabella_Tab_Check_Imp = "Cli",
                                Chiave_Record_Tab_Check_Imp = oRow.Codice_Cliente,
                            });
                            log.Warn("Codice Cliente cui si rifericono gli errori precedenti: " + oRow.Codice_Cliente + " -------------------------------------------------------------------------------");
                            errorsList.Add(currentDictionary);
                            errorsList.Add(importDictionary);
                            }
                        }
                    catch (Exception ex)
                        {
                            var RRNERR = oRow.Codice_Cliente;
                            //Carica nel WARN LOG il Record Access nel caso in cui sia Alzato il Flag PrintDetailInImportAccess nei Settings di Common/Properties
                            if (Common.Properties.Settings.Default.PrintRecordAccessInErrorImport)
                                Log.ErrorFormat("TAB CLI - Chiave: {0}", oRow.Codice_Cliente);
                            throw ex;
                        }
                }
                RepoManager.Tab_Chk_ImpRepo.BeginWork();
                try
                {
                    BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                         BusinessService.GetLocalizedString(PowerWebResources.STR_STO_SCRIVENDO_NEL_DATABASE_TAB_X_LASTKEY_Y.ToString(), "CLI", lastKey));
                    RepoManager.CliRepo.Add(toImport);
                    RepoManager.Tab_Chk_ImpRepo.Add(errors);
                    if (errors.Count == 0)
                        RepoManager.Tab_Chk_ImpRepo.Add(new Tab_Chk_Imp
                        {
                            Nome_Tabella_Tab_Check_Imp = "Cli",
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
    }
}