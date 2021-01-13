using Business;
using Business.LicenceServiceReference;
using Business.Repository;
using Common;
using Domain;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerWeb.Modules
{
    public static class EditDictionaryManager
    {

        private static Versioni ass_domiciliare_versione = RepoManager.VersioniRepo.FirstOrDefault(v => v.Nome_Versioni == "TD_VERSIONE_ASSISTENZA_DOMICILIARE");

        static List<Tab_EditFormTemplate> EditFormTemplates
        {
            get
            {
                var templates = PowerWebContext.GetFromSession<List<Tab_EditFormTemplate>>(CommonService.SESS_EDITFORMTEMPLATES);
                if (templates == null)
                {
                    templates = RepoManager.Tab_EditFormTemplateRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_EditFormTemplate>>(CommonService.SESS_EDITFORMTEMPLATES, templates);
                }
                return templates;

            }

        }

        private static bool IsEFTTabDisabled(Tab_EditFormTemplate currentEFT, string tabName)
        {
            bool isDisabled = false;
            if (currentEFT != null && currentEFT.Tabs_Tab_EditFormTemplate != null)
            {
                string[] chuncks = currentEFT.Tabs_Tab_EditFormTemplate.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                isDisabled = chuncks.Contains(tabName);
            }

            return isDisabled;
        }

        private static TabPageItemExtended GetEFTField(Tab_EditFormTemplate currentEFT, string fieldName, TabPageItemFieldTypeEnum PIFEnum = TabPageItemFieldTypeEnum.None, int columnSpan = 0)
        {
            bool isDisabled = false;
            if (currentEFT != null && currentEFT.Fields_Tab_EditFormTemplate != null)
            {

                string[] chuncks = currentEFT.Fields_Tab_EditFormTemplate.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                isDisabled = chuncks.Contains(fieldName);
            }

            return isDisabled ? new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField) : new TabPageItemExtended(fieldName, PIFEnum, columnSpan);
        }

        private static JObject GetEFTFieldJson(Tab_EditFormTemplate currentEFT, string fieldName, bool extra = false)
        {
            JObject ret = new JObject();
            bool isDisabled = false;

            if (extra == true)
            {
                JObject edOptions;
                switch (fieldName)
                {
                    case "Cognome_Assistito_Can":

                        edOptions = new JObject();
                        edOptions.Add("value", "");
                        ret.Add("editorOptions", edOptions);
                        break;
                    case "Tipo_Interv_Can":
                        edOptions = new JObject();
                        edOptions.Add("value", "");
                        ret.Add("editorOptions", edOptions);
                        break;
                }
            }

            if (currentEFT != null && currentEFT.Fields_Tab_EditFormTemplate != null)
            {
                string[] chuncks = currentEFT.Fields_Tab_EditFormTemplate.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                isDisabled = chuncks.Contains(fieldName);
            }
            if (isDisabled)
            {
                ret.Add("itemType", "empty");
            }
            else
            {
                ret.Add("dataField", fieldName);
                ret.Add("editorType", "dxTextBox");
            }

            return ret;
        }

        // Gestione dei TAB dei Moduli che li hanno

        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCant_Fru()
        {
            Fru_Cant _fru_CantStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Fru_Cant).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();


            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };

            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Abilitazione_Data_Inizio_Fru_Can)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.DisAbilitazione_Fru_Can)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Fru_Id)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.N_Serie_Fru)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Data_Registrazione_Fru_Can)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.DataOraUltimaModifica_Fru_Can)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }

            TabPageExtended NoteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, NoteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Note_Fru_Can)));

                templateDic.Add(NoteTPE, noteTabList);
            }

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCant_Note()
        {
            Cant_Note _cant_NoteStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Cant_Note).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di CANT_NOTE
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.Tipo_Nota_Can_Note)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.Data_Nota_Can_Note)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.Nota_Can_Note)));
                infoTabList.Add(GetEFTField(currentEFT, null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.Utenti_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.DisAbilitazione_Can_Note)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.Data_Registrazione_Can_Note)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cant_NoteStub.DataOraUltimaModifica_Can_Note)));

                templateDic.Add(generaliTPE, infoTabList);
            }

            #endregion
            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCant()
        {
            Cant oCant = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Cant).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            //Se l'utente ha la versione di assistenza domiciliare, inserisce in testa la tab ASSISTITO
            if (ass_domiciliare_versione != default(Versioni) && PowerWebContext.Current.Versione.Versioni_Id == ass_domiciliare_versione.Versioni_Id)
            {
                #region Gestione Lista dei Campi del TAB : ASSISTITO di CANT
                TabPageExtended assistitoTPE = new TabPageExtended
                {
                    Name = "Assistito",
                    Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ASSISTITO),
                    Columns = 3,
                };
                if (!IsEFTTabDisabled(currentEFT, assistitoTPE.Name))
                {
                    List<TabPageItemExtended> oAssistitoTabList = new List<TabPageItemExtended>();
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cognome_Assistito_Can)));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Nome_Assistito_Can)));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Livello_Assistito_Can)));

                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Luogo_Nascita_Can)));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cap_Nascita_Can)));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Provincia_Nascita_Can)));

                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Nascita_Can)));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));


                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Residenza_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Residenza_Localita_Can)));

                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Residenza_Interno_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    //oAssistitoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Nascita_Can)));
                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));

                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Sesso_Can)));

                    // per l'edit form template di Mosaico questi due campi sono visualizzati nella tab "Generali" e quindi qui non sono visualizzati
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantEditFormTemplateEnum);
                    if (customizationVersion != (int)CantEditFormTemplateEnum.Mosaico)
                    {
                        oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cod_Fisc_Can), TabPageItemFieldTypeEnum.NotInGroup));
                        oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Voucher_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    }

                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Rapporto_Inizio_1_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Rapporto_Fine_1_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    oAssistitoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    templateDic.Add(assistitoTPE, oAssistitoTabList);
                }
                #endregion
            }
            #region Gestione Lista dei Campi del TAB : GENERALI di CANT
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };

            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Cantiere)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Descrizione_Can)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tipologia_Can)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cli_Id)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Fil_Id)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.DisAbilitazione_Can)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Raggruppamento1_Can)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Raggruppamento2_Can)));

                // per l'edit form template di Mosaico il codice voucher e il codice fiscale sono visualizzati nella tab generali
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantEditFormTemplateEnum);
                if (customizationVersion == (int)CantEditFormTemplateEnum.Mosaico)
                {
                    oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cod_Fisc_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Voucher_Can), TabPageItemFieldTypeEnum.NotInGroup));
                }

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Registrazione_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.DataOraUltimaModifica_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Singola_Reg)));
                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }



            #endregion
            #region Gestione Lista dei Campi del TAB : UBICAZIONE di CANT
            TabPageExtended ubicazioneTPE = new TabPageExtended
            {
                Name = "Ubicazione",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_UBICAZIONE),
                Columns = 2,
            };

            if (!IsEFTTabDisabled(currentEFT, ubicazioneTPE.Name))
            {
                List<TabPageItemExtended> oUbicazioneTabList = new List<TabPageItemExtended>();
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Provincia_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Luogo_Can)));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Cap_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Indirizzo_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oUbicazioneTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Frazione_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Provincia_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Cap_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Luogo_Can)));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Domicilio_Luogo_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Localita_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Indirizzo_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Interno_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Can)));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Zona_Can)));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.FlagGps_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.DataVarGps_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.RaggioGps_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.LatitudineGps_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oUbicazioneTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.LongitudineGps_Can), TabPageItemFieldTypeEnum.NotInGroup));


                templateDic.Add(ubicazioneTPE, oUbicazioneTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : TEL/FAX di CANT

            TabPageExtended telfaxTPE = new TabPageExtended
            {
                Name = "TelFax",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_TELEFONI_FAX),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, telfaxTPE.Name))
            {
                List<TabPageItemExtended> oTelefoniFaxTabList = new List<TabPageItemExtended>();
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_1_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_1_Rif_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_2_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_2_Rif_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_1_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_1_Rif_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_2_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oTelefoniFaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_2_Rif_Can), TabPageItemFieldTypeEnum.NotInGroup));
                templateDic.Add(telfaxTPE, oTelefoniFaxTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : PARAMETRI di CANT

            TabPageExtended parametriTPE = new TabPageExtended
            {
                Name = "Parametri",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_PARAMETRI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, parametriTPE.Name))
            {
                List<TabPageItemExtended> oParametriTabList = new List<TabPageItemExtended>();
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Min_Ril_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Ril_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Gruppo_Ril_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Gruppo_Notte_Ril_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.TipoNotturno_Can)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Inizio_Notte_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Notturno_Can)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Numero_GG_Lavorativi), TabPageItemFieldTypeEnum.NotInGroup));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Cantiere_Can)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Flag_NON_Esportare_Can)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Ore_Massime_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Calcolo_Viaggi_Can)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Interv_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Costo_Orario_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tab_Orari_Tipo_Id), TabPageItemFieldTypeEnum.NotInGroup));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tempo_Attivita_Can), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(parametriTPE, oParametriTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : ARROTONDAMENTI di CANT

            TabPageExtended arrotondamentiTPE = new TabPageExtended
            {
                Name = "Arrotondamenti",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ARROTONDAMENTI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, arrotondamentiTPE.Name))
            {
                List<TabPageItemExtended> oArrotondamentiTabList = new List<TabPageItemExtended>();
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Metodo_Arrotondamento_Can)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Arrotondamento_Can)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Minuti_Tolleranza_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Arrot_Fig_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Minuti_Tolleranza_F_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Arrot_Fig_F_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Arrot_Durata_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Durata_Can), TabPageItemFieldTypeEnum.NotInGroup));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Entrata_Mattina_Cant), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Entrata_Pomeriggio_Cant), TabPageItemFieldTypeEnum.NotInGroup));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Uscita_Mattina_Cant), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Uscita_Pomeriggio_Cant), TabPageItemFieldTypeEnum.NotInGroup));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tolleranza_Limite_Uscita_Mattina_Cant), TabPageItemFieldTypeEnum.NotInGroup));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tolleranza_Limite_Uscita_Pomeriggio_Cant), TabPageItemFieldTypeEnum.NotInGroup));


                oArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Tolleranza_Limite_Entrata_Pomeriggio_Cant), TabPageItemFieldTypeEnum.NotInGroup));


                templateDic.Add(arrotondamentiTPE, oArrotondamentiTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : NOTE di CANT
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> oNoteTabList = new List<TabPageItemExtended>();
                oNoteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Note_Can), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, oNoteTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : IMPORTI di CANT
            TabPageExtended importiTPE = new TabPageExtended
            {
                Name = "Importi",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORTI),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, importiTPE.Name))
            {
                List<TabPageItemExtended> oImportiTabList = new List<TabPageItemExtended>();

                templateDic.Add(importiTPE, oImportiTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : TURNI di CANT
            TabPageExtended turniTPE = new TabPageExtended
            {
                Name = "Turni",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_TURNI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, turniTPE.Name))
            {
                List<TabPageItemExtended> oTurniTabList = new List<TabPageItemExtended>();

                templateDic.Add(turniTPE, oTurniTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : SERVIZI di CANT
            TabPageExtended serviziTPE = new TabPageExtended
            {
                Name = "Servizi",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_SERVIZI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, serviziTPE.Name))
            {
                List<TabPageItemExtended> oServiziTabList = new List<TabPageItemExtended>();

                oServiziTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Commessa_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oServiziTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Gestionale_Can), TabPageItemFieldTypeEnum.NotInGroup));
                oServiziTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(serviziTPE, oServiziTabList);
            }

            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCli()
        {
            Cli _cliStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Cli).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Gestione Lista dei Campi del TAB : GENERALI di CLI
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> genTabList = new List<TabPageItemExtended>();
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Codice_Cliente)));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Cognome_Cli)));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Nome_Cli)));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.DisAbilitazione_Cli)));

                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Codice_Ditta_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Codice_Fiscale_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Partita_Iva_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                genTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Data_Registrazione_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                genTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.DataOraUltimaModifica_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                genTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                genTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(generaliTPE, genTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : CONDIZIONI COMMERCIALI di CLI
            TabPageExtended condCommTPE = new TabPageExtended
            {
                Name = "CondComm",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_COND_COMM),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, condCommTPE.Name))
            {
                List<TabPageItemExtended> condCommTabList = new List<TabPageItemExtended>();
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_Codice_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_GG_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_GF_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_Rate_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_Intervallo_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_1Mese_Escl_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_2Mese_Escl_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Pagamento_Sconto_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Banca_Iban_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Banca_Descrizione_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Lingua_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Valuta_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Data_Rapporto_Inizio_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Data_Rapporto_Fine_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                condCommTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                condCommTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(condCommTPE, condCommTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : INDIRIZZI di CLI
            TabPageExtended indirizziTPE = new TabPageExtended
            {
                Name = "Indirizzi",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_INDIRIZZI),
                Columns = 3,
            };
            if (!IsEFTTabDisabled(currentEFT, indirizziTPE.Name))
            {
                List<TabPageItemExtended> indTabList = new List<TabPageItemExtended>();
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Residenza_Nazione_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Residenza_Provincia_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Residenza_Cap_Cli)));

                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Residenza_Luogo_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Residenza_Indirizzo_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                indTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Domicilio_Nazione_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Domicilio_Provincia_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Domicilio_Cap_Cli)));

                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Domicilio_Luogo_Cli)));
                indTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Domicilio_Indirizzo_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                indTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(indirizziTPE, indTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : TELEFONI & FAX di CLI
            TabPageExtended telFaxTPE = new TabPageExtended
            {
                Name = "TelFax",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_TELEFONI_FAX),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, telFaxTPE.Name))
            {
                List<TabPageItemExtended> telefaxTabList = new List<TabPageItemExtended>();
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_1_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_1_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_2_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_2_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_3_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_3_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_4_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Telefono_4_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Fax_1_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Fax_1_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Fax_2_Cli), TabPageItemFieldTypeEnum.NotInGroup));
                telefaxTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Fax_2_Rif_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(telFaxTPE, telefaxTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : NOTE di CLI
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _cliStub.Note_Cli), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCol_Note()
        {
            Col_Note _col_NoteStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Col_Note).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.Tipo_Nota_Col_Note)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.Data_Nota_Col_Note)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.Nota_Col_Note)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.Utenti_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.DisAbilitazione_Col_Note)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.Data_Registrazione_Col_Note)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_NoteStub.DataOraUltimaModifica_Col_Note)));

                templateDic.Add(generaliTPE, infoTabList);
            }

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCol_Pru()
        {
            Pru_Col _col_PruStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Pru_Col).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.Abilitazione_Data_Inizio_Pru_Col)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.DisAbilitazione_Pru_Col)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.Pru_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.N_Serie_Pru)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.Data_Registrazione_Pru_Col)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.DataOraUltimaModifica_Pru_Col)));

                templateDic.Add(generaliTPE, infoTabList);

            }

            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _col_PruStub.Note_Pru_Col)));



                templateDic.Add(noteTPE, noteTabList);
            }
            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryCol()
        {
            Col _colStub = null;

            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EditManager);

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Col).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Gestione Lista dei Campi del TAB : GENERALI di COL
            TabPageExtended generaliTPE = new TabPageExtended
            {

                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> GeneraliTabList = new List<TabPageItemExtended>();
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Collaboratore)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Resp_Id)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Cognome_Col)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nome_Col)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Data_Registrazione_Col), TabPageItemFieldTypeEnum.NotInGroup));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.DataOraUltimaModifica_Col), TabPageItemFieldTypeEnum.NotInGroup));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Raggruppamento1_Col)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Raggruppamento2_Col)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Singola_Reg)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.DisAbilitazione_Col)));

                templateDic.Add(generaliTPE, GeneraliTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : ANAGRAFICA di COL
            TabPageExtended anagraficaTPE = new TabPageExtended
            {
                Name = "Anagrafica",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ANAGRAFICA),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, anagraficaTPE.Name))
            {
                List<TabPageItemExtended> AnagrTabList = new List<TabPageItemExtended>();
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nazionalita_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Straniero_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Straniero_CEE_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Straniero_Scadenza_Permesso_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nascita_Luogo_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nascita_Cap_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nascita_Provincia_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Nascita_Luogo_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Nascita_Data_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Sesso_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Stato_Civile_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Fiscale_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Titolo_Studio_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Automunito_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Patente_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Scadenza_Patente_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.N_Persone_A_Carico_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Assegni_Famigliari_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_INPS_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.N_Pos_INPS_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.N_Pos_INAIL_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Matricola_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Libretto_Sanitario_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Data_Sorv_San_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Disabile_Col)));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Quota_PTime_Col)));

                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Iban_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AnagrTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Data_Proroga_Contratto), TabPageItemFieldTypeEnum.NotInGroup));
                //AnagrTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(anagraficaTPE, AnagrTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : QUALIFICHE di COL
            TabPageExtended qualificheTPE = new TabPageExtended
            {
                Name = "Qualifiche",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_QUALIFICHE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, qualificheTPE.Name))
            {
                List<TabPageItemExtended> QualTabList = new List<TabPageItemExtended>();
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Prova)));
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Qualifica_Col)));

                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Inizio_Col)));
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Data_Disponibilita_Fine_Col)));

                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Livello_Col)));
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tipo_Rapporto_Col)));

                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tipo_Col)));
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tipo_Contratto_Col)));

                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Cant_Id)));
                QualTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tab_Orari_Tipo_Id)));

                templateDic.Add(qualificheTPE, QualTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : INDIRIZZI di COL
            TabPageExtended indirizziTPE = new TabPageExtended
            {
                Name = "Indirizzi",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_INDIRIZZI),
                Columns = 3,
            };
            if (!IsEFTTabDisabled(currentEFT, indirizziTPE.Name))
            {
                List<TabPageItemExtended> AddressTabList = new List<TabPageItemExtended>();

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Provincia_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Cap_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Luogo_Col)));

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Residenza_Luogo_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Localita_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Indirizzo_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AddressTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                AddressTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Interno_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Provincia_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Cap_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Luogo_Col)));

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Codice_Domicilio_Luogo_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Localita_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Indirizzo_Col), TabPageItemFieldTypeEnum.NotInGroup));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Domicilio_Interno_Col), TabPageItemFieldTypeEnum.NotInGroup));

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Residenza_Provincia_GEN_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Zona_Col)));

                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.LatitudineGps_Col)));
                AddressTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.LongitudineGps_Col)));

                AddressTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                AddressTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                AddressTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(indirizziTPE, AddressTabList);
            }

            #endregion
            #region Gestione Lista dei Campi del TAB : TEL&FAX di COL
            TabPageExtended telFaxTPE = new TabPageExtended
            {
                Name = "TelFax",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_TELEFONI_FAX),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, telFaxTPE.Name))
            {
                List<TabPageItemExtended> TelTabList = new List<TabPageItemExtended>();
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_1_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_1_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_2_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_2_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));

                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_3_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_3_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_4_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Telefono_4_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));

                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fax_1_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fax_1_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fax_2_Col), TabPageItemFieldTypeEnum.NotInGroup));
                TelTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fax_2_Rif_Col), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(telFaxTPE, TelTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : PARAMETRI di COL
            TabPageExtended parametriTPE = new TabPageExtended
            {
                Name = "Parametri",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_PARAMETRI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, parametriTPE.Name))
            {

                if (customizationVersion == (int)EditManager.ServiziItalia)
                {
                    List<TabPageItemExtended> cParametriTabList = new List<TabPageItemExtended>();
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Min_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Gruppo_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Gruppo_Notte_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.TipoNotturno_Col)));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_Monte_Ore)));


                    templateDic.Add(parametriTPE, cParametriTabList);
                }
                else if (customizationVersion != (int)EditManager.ServiziItalia)
                {
                    List<TabPageItemExtended> cParametriTabList = new List<TabPageItemExtended>();
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Min_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Gruppo_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Max_Gruppo_Notte_Ril_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.TipoNotturno_Col)));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Inizio_Notte_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Notturno_Col)));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_NON_Esportare_Col)));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.GGConsMax_Col), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.OreMaxGG_Col), TabPageItemFieldTypeEnum.NotInGroup));

                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.OreMassime), TabPageItemFieldTypeEnum.NotInGroup));
                    cParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_Monte_Ore)));

                    templateDic.Add(parametriTPE, cParametriTabList);

                }


            }
            #endregion
            #region Gestione Lista dei Campi del TAB : ARROTONDAMENTI di COL
            TabPageExtended arrotondamentiTPE = new TabPageExtended
            {
                Name = "Arrotondamenti",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ARROTONDAMENTI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, arrotondamentiTPE.Name))
            {
                List<TabPageItemExtended> ArrotondamentiTabList = new List<TabPageItemExtended>();
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Metodo_Arrotondamento_Col)));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tipo_Arrotondamento_Col)));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.ArrotI_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.SogliaI_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.ArrotF_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.SogliaF_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Arrot_Durata_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Soglia_Durata_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Soglia_Minima_Arrotondamento_Durata_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Entrata_Mattina_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Entrata_Pomeriggio_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Uscita_Mattina_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Uscita_Pomeriggio_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tolleranza_Limite_Uscita_Mattina_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tolleranza_Limite_Uscita_Pomeriggio_Col), TabPageItemFieldTypeEnum.NotInGroup));


                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Ritardo_Tolleranza_Minuti_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Tolleranza_Limite_Entrata_Pomeriggio_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Limite_Entrata_Inizio_Pomeriggio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(arrotondamentiTPE, ArrotondamentiTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : NOTE di COL
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> NoteTabList = new List<TabPageItemExtended>();
                NoteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Note_Col), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, NoteTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : IMPORTI di COL
            TabPageExtended importiTPE = new TabPageExtended
            {
                Name = "Importi",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORTI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, importiTPE.Name))
            {
                List<TabPageItemExtended> ParametriTabList = new List<TabPageItemExtended>();

                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Retribuzione_Oraria_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Retribuzione_Straordinaria_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Retribuzione_Lorda_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Retribuzione_Netta_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Trattenuta_Vitto_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Indennita_Sanificazione_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Indennita_Trasporto_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ParametriTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(importiTPE, ParametriTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : VIAGGI&PAUSE di COL
            TabPageExtended viaggiePauseTPE = new TabPageExtended
            {
                Name = BusinessService.GetLocalizedString(PowerWebResources.STR_VIAGGI_E_PAUSE),
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_VIAGGI_E_PAUSE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, viaggiePauseTPE.Name))
            {
                List<TabPageItemExtended> ViaggiTabList = new List<TabPageItemExtended>();
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Minuti_Arrot_Durata_Fig_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Soglia_Arrot_Durata_Fig_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_Ore_Viaggi_Col)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Durata_Pausa_Col)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_Ore_Viaggi_Col_Inizio_Fine)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Flag_Viaggio_InizioFine_GIS)));
                // ViaggiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_1_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_1_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_2_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_2_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_3_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_3_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_4_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_4_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_5_Inizio_Col), TabPageItemFieldTypeEnum.NotInGroup));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _colStub.Fascia_Ore_Viaggi_5_Fine_Col), TabPageItemFieldTypeEnum.NotInGroup));


                templateDic.Add(viaggiePauseTPE, ViaggiTabList);
            }
            #endregion
            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryFil()
        {
            Fil _filStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Fil).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Gestione Lista dei Campi del TAB : GENERALI di FIL
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.Codice_Fil)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.Descrizione_Fil)));

                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.DisAbilitazione_Fil)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.Data_Registrazione_Fil)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.DataOraUltimaModifica_Fil)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            #region Note
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _filStub.Note_Fil), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryFil_Utenti()
        {
            Utenti_Fil _fil_UtentiStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Utenti_Fil).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fil_UtentiStub.Utenti_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fil_UtentiStub.Dominio_Utenti_Fil)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryFru_Cant()
        {
            Fru_Cant _fru_CantStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Fru_Cant).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Abilitazione_Data_Inizio_Fru_Can)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.DisAbilitazione_Fru_Can)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Cant_Id)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.DataOraUltimaModifica_Fru_Can)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Data_Registrazione_Fru_Can)));

                templateDic.Add(generaliTPE, infoTabList);
            }

            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fru_CantStub.Note_Fru_Can)));

                templateDic.Add(noteTPE, noteTabList);
            }

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryFru()
        {
            Fru _fruStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Fru).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.Codice_Fru)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.N_Serie_Fru)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.Singola_Reg_Fru)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.DisAbilitazione_Fru)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.Data_Registrazione_Fru), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.DataOraUltimaModifica_Fru), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion
            #region Note
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _fruStub.Note_Fru), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryLingue()
        {
            Lingue _lingueStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Lingue).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di FIL
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _lingueStub.Sigla_Lingue)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _lingueStub.Nome_Lingue)));

                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _lingueStub.DisAbilitazione_Lingue)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _lingueStub.Data_Registrazione_Lingue)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _lingueStub.DataOraUltimaModifica_Lingue)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryParam()
        {
            Param _paramStub = null;
            //Legge dalla TAB_EDITFORMTEMPLATE gli eventuali TAB Disabilitati
            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Param).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di PARAM
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.CompanyName)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.CompanyAddress)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.CompanyInfo)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.CompanyEmail)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Mesi_Validita_Reg)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Codice_Cliente)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.GG_X_Allarmi)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tempo_Doppia_Reg)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.MDBPath)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.PercorsoFotoCol)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Data_Registrazione)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.DataOraUltimaModifica)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : DEFAULT di PARAM
            TabPageExtended defaultTPE = new TabPageExtended
            {
                Name = "Default",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_DEFAULT),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, defaultTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> oDefaultTabList = new List<TabPageItemExtended>();
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Dflt_DoRefresh_Grid)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Dflt_Include_Activities)));

                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Dflt_Include_Pass)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Dflt_Include_Trips)));

                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Dflt_Include_Blocked)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Sabato_Feriale)));

                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.RowsPerPage)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.ComboboxRowsPerPage)));

                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.ComboBoxDelay)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.RowsPerComboBox)));

                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.RaggioGpsDefault)));
                oDefaultTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.PasswordExpirationDays)));

                templateDic.Add(defaultTPE, oDefaultTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : ATTIVAZIONI di PARAM
            TabPageExtended attivazioniTPE = new TabPageExtended
            {
                Name = "Attivazioni",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ATTIVAZIONI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, attivazioniTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> oAttivazioniTabList = new List<TabPageItemExtended>();

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Attiva_Num_Aut_Can)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Attiva_Num_Aut_Col)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Arrotondamenti)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Att)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Orari)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Cartellino)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Monte_Minuti)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Notturno)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Viaggi)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Schedulatore)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_GPS)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Confronto_Ore_Budget)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Pass)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Notifiche)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Where_Is_It)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Aut_Str)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Privacy)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.ImportaFileTimbratureSospese)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.ModuleType)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.DomainFilter)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Ctrl_Codice_Fisc)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Ctrl_Codice_IBAN)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Ctrl_Tab_Comuni)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Ctrl_Sovrap_Pass)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.File_Cant_Var)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.File_Col_Var)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Aut_Str_Tipo)));
                oAttivazioniTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Import_Esterno)));
                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Abilita_Sincronizzazione_Entità)));

                oAttivazioniTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.BlockLoginOnUserPswExpired)));
                oAttivazioniTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(attivazioniTPE, oAttivazioniTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : PARAMETRI di PARAM
            TabPageExtended parametriTPE = new TabPageExtended
            {
                Name = "Parametri",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_PARAMETRI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, parametriTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> oParametriTabList = new List<TabPageItemExtended>();
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Durata_Min_Ril)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Durata_Max_Ril)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Durata_Max_Gruppo_Ril)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Durata_Max_Gruppo_Notte_Ril)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.TipoNotturno)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Durata_Notturno)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Durata_Giornata)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Ora_Inizio_Giornata)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Ass_Tag_Gps)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Importazione_timbrature_GPS)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cantiere_Timbrature_GPS_Non_Valide)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Chiusura_Causali)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.MaxElab_ImportChunkSize)));
                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Sincronizzazione_ClockApp)));

                oParametriTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Url_ClockAppsManager)));
                oParametriTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(parametriTPE, oParametriTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : ARROTONDAMENTI di PARAM
            TabPageExtended arrotondamentiTPE = new TabPageExtended
            {
                Name = "Arrotondamenti",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_ARROTONDAMENTI),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, arrotondamentiTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> oArrotondamentiTabList = new List<TabPageItemExtended>();
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Metodo_Arrotondamento)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Arrotondamento)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Minuti_Arrot_I)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Soglia_Arrot_I)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Minuti_Arrot_F)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Soglia_Arrot_F)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Minuti_Durata)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Default_Soglia_Durata)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Soglia_Minima_Arrotondamento_Durata)));
                oArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Utilizzo_Limite_Entrata)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Utilizzo_Limite_Uscita)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Ritardo_Tolleranza_Minuti)));
                oArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Entrata_Mattina)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Entrata_Pomeriggio)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Uscita_Mattina)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Uscita_Pomeriggio)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Entrata_Usa_Orario)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Uscita_Usa_Orario)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tolleranza_Limite_Uscita_Mattina)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tolleranza_Limite_Uscita_Pomeriggio)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tolleranza_Limite_Entrata_Pomeriggio)));
                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Limite_Entrata_Inizio_Pomeriggio)));

                oArrotondamentiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tolleranza_Limite_Entrata)));
                oArrotondamentiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(arrotondamentiTPE, oArrotondamentiTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : VIAGGI E PAUSE di PARAM
            TabPageExtended viaggiTPE = new TabPageExtended
            {
                Name = "ViaggiePause",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_VIAGGI_E_PAUSE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, viaggiTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> ViaggiTabList = new List<TabPageItemExtended>();

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Flag_GPS)));
                ViaggiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Assegnazione_KMMinuti)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Flag_Ore_Viaggi)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Flag_Ore_Viaggi_Inizio_Fine)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.BingKey)));
                ViaggiTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Pausa_Viaggio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Tipo_Viaggio)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Durata_Minima_Viaggio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Durata_Massima_Viaggio)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cant_Id)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Durata_Pausa)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Utilizzo_Fasce_Viaggi)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Flag_Calcolo_Viaggi)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_1_Inizio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_1_Fine)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_2_Inizio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_2_Fine)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_3_Inizio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_3_Fine)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_4_Inizio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_4_Fine)));

                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_5_Inizio)));
                ViaggiTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Fascia_Ore_Viaggi_5_Fine)));

                templateDic.Add(viaggiTPE, ViaggiTabList);
            }
            #endregion
            #region Gestione Lista dei Campi del TAB : CARTELLINO di PARAM
            TabPageExtended cartellinoTPE = new TabPageExtended
            {
                Name = "Cartellino",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_CARTELLINO),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, cartellinoTPE.Name))
            //Visualizza questo TAB se NON risulta Disabilitato (ovvero NON è presente nella TAB_EDITFORMTEMPLATE)
            {
                List<TabPageItemExtended> CartellinoTabList = new List<TabPageItemExtended>();

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Flag_Monte_Ore)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Divisione_Piano_Notturno_Diurno)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Inizio_Notturno)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Fine_Notturno)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Piano)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Ore)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Motivazioni)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Delta)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Viaggi)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Totale)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Visualizza_Totali_Settimanali)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Usa_Rettifiche_Auto)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Abilita_Stampa_Cart_Editabile)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Usa_Rettifiche_Manuali)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Modalita_Compatta)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Totale_Prima_Colonna)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Usa_Cartellino_Modificabile)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Abilita_Divisione_Cantiere)));

                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Durata_Quadratura_Rettifiche_Auto)));
                CartellinoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _paramStub.Cartellino_Abilita_Modifica)));
                templateDic.Add(cartellinoTPE, CartellinoTabList);
            }

            #endregion
            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryPru_Col()
        {
            Pru_Col _pru_ColStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Pru_Col).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();


            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.Abilitazione_Data_Inizio_Pru_Col)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.DisAbilitazione_Pru_Col)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.Col_Id)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.DataOraUltimaModifica_Pru_Col)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.Data_Registrazione_Pru_Col)));

                templateDic.Add(generaliTPE, infoTabList);
            }

            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pru_ColStub.Note_Pru_Col)));

                templateDic.Add(noteTPE, noteTabList);
            }

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryPendingElab()
        {
            PendingElab _pendingElabStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(PendingElab).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pendingElabStub.ElaborateDate_PendingElab)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pendingElabStub.FromDate_PendingElab)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pendingElabStub.ToDate_PendingElab)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pendingElabStub.Fru_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pendingElabStub.Pru_Id)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryPru()
        {
            Pru _pruStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Pru).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.Codice_Pru)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.N_Serie_Pru)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.Singola_Reg_Pru)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.DisAbilitazione_Pru)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.Data_Registrazione_Pru), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.DataOraUltimaModifica_Pru), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion
            #region Note
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _pruStub.Note_Pru), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryRegV()
        {
            Reg_V _regvStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Reg_V).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                bool hideLombardaUnusedFields = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideLombardaUnusedFieldsRegVEnum) == (int)HideLombardaUnusedFieldsRegVEnum.Hide;

                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Col_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Cant_Id)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Reg)));
                if (hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)));
                }
                else
                {
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                }

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_E)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_U)));

                // se il notturno risulta abilitato nella scheda parametri
                // allora si visualizza anche il checkbox che serve a capire se l'ora d'uscita inferiore all'ora d'entrata
                // fa parte dello stesso giorno
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno
                    && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None & RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled
                    && !hideLombardaUnusedFields)
                {
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.IsUTimeSameDayE)));
                }

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_E)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fig_U)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.IsOnlyDuration)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Durata_Fis_HH_S)));

                if (!hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.RegistrationDurationNegative)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Durata_Fig_HH_S)));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Fru_Id)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Pru_Id)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Codice_Cliente)));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.KM_Reg)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Tipo_Modifica)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.IsNotToElaborate)));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Stato_Reg)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.RiferimentoRRN_Att)));
                }

                // verifico la personalizzazione per la visualizzazione dei flag E/U
                int showEuCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowFlagEUInFormEnum);

                // si visualizza il flag di entrata/uscita solamente se è previsto dalle personalizzazioni
                if (showEuCustomization == (int)ShowFlagEUInFormEnum.Show)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.EntrataEU)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.UscitaEU)));
                }

                if (!hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Bloccata)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Note_Reg), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                }

                // se è abilitato il modulo gps si portano anche le latitudini e longitudini
                if (RepoManager.ParamRepo.ParametersRow.Abilita_GPS)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Lat_Orig_E)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Long_Orig_E)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Lat_Orig_U)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Long_Orig_U)));
                }

                int activityEvaluationCust = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableActivityEvaluationEnum);

                if (activityEvaluationCust == (int)EnableActivityEvaluationEnum.Enabled)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Activity_Evaluation)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                }

                templateDic.Add(generaliTPE, infoTabList);
            }

            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryRegVM()
        {
            Reg_V _regvStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Reg_V).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                bool hideLombardaUnusedFields = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.HideLombardaUnusedFieldsRegVEnum) == (int)HideLombardaUnusedFieldsRegVEnum.Hide;

                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Col_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Cant_Id)));

                List<Cli> clienti = RepoManager.CliRepo.GetAll().ToList();

                //se vi sono dei clienti
                if (clienti.Any() && !hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Codice_Cliente)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Cognome_Cli)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Nome_Cli)));
                }


                if (hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Reg)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id)));
                }
                else
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Reg), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Motivazione_Reg_Id), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.Solaris) == 1)
                    {
                        infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.CentroDiCosto_Id), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                        infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    }

                }

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_E)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Data_Ora_Fis_U)));

                // se il notturno risulta abilitato nella scheda parametri
                // allora si visualizza anche il checkbox che serve a capire se l'ora d'uscita inferiore all'ora d'entrata
                // fa parte dello stesso giorno
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno &&
                    RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled &&
                    !hideLombardaUnusedFields)
                {
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.IsUTimeSameDayE)));
                }

                if (!hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Fru_Id)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Pru_Id)));
                }

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.IsOnlyDuration)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Durata_Fis_HH_S)));

                if (!hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.RegistrationDurationNegative)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Tipo_Reg)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Stato_Reg)));
                }

                // verifico la personalizzazione per la visualizzazione dei flag E/U
                int showEuCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowFlagEUInFormEnum);

                // si visualizza il flag di entrata/uscita solamente se è previsto dalle personalizzazioni
                if (showEuCustomization == (int)ShowFlagEUInFormEnum.Show)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.EntrataEU)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.UscitaEU)));
                }


                if (!hideLombardaUnusedFields)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Bloccata)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Note_Reg), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                }

                // se è abilitato il modulo gps si portano anche le latitudini e longitudini
                if (RepoManager.ParamRepo.ParametersRow.Abilita_GPS && PowerWebContext.Current.IsSupervised)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Lat_Orig_E)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Long_Orig_E)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Lat_Orig_U)));
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Registrazione_Long_Orig_U)));
                }

                int activityEvaluationCust = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableActivityEvaluationEnum);

                if (activityEvaluationCust == (int)EnableActivityEvaluationEnum.Enabled)
                {
                    infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _regvStub.Activity_Evaluation)));
                    infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                }


                templateDic.Add(generaliTPE, infoTabList);
            }

            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryResp()
        {
            Resp _respStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Resp).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di FIL
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.Codice_Resp)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.Descrizione_Resp)));

                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.DisAbilitazione_Resp)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.Data_Registrazione_Resp)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.DataOraUltimaModifica_Resp)));
                #endregion
                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #region Note
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 1,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _respStub.Note_Resp), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion
            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryResources()
        {
            Resources _resourcesStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Resources).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di RESOURCES
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resourcesStub.Lingue_Id)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resourcesStub.Versioni_Id)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resourcesStub.ResourceKey), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resourcesStub.ResourceValue), TabPageItemFieldTypeEnum.ColumnSpan, 2));
                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryResp_Utenti()
        {
            Utenti_Resp _resp_Utenti_Stub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Utenti_Resp).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resp_Utenti_Stub.Utenti_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _resp_Utenti_Stub.Dominio_Utenti_Resp)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionarySchedule()
        {
            ScheduleData scheduleDataStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(ScheduleData).Name);

            var templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                var infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleType)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.JobType)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleCreationDateTime), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleEditDateTime), TabPageItemFieldTypeEnum.NotInGroup));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleParameters)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleExclusions)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.JobParameters)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleStartDateTime), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => scheduleDataStub.ScheduleEndDateTime), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTabAut()
        {
            Tab_Aut _tab_AutStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Aut).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 4,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> GeneraliTabList = new List<TabPageItemExtended>();
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Tab_Funz_Id)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Utenti_Id)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.DataOraUltimaModifica_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Data_Registrazione_Aut)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Funz_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Ins_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Mod_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.Del_Aut)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgIns1_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgIns2_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgMod1_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgMod2_Aut)));

                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgDel1_Aut)));
                GeneraliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_AutStub.MsgDel2_Aut)));
                GeneraliTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                GeneraliTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(generaliTPE, GeneraliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Comuni()
        {
            Tab_Comuni _tab_ComuniStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Comuni).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di FIL
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Luogo_Tab_Comuni)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Cap_Tab_Comuni)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Tab_Prov_Id)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Codice_Prov_Tab_Comuni)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Codice_Luogo_Tab_Comuni)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ComuniStub.Codice_Istat_Tab_Comuni)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Decod()
        {
            Tab_Decod _tab_Decodtub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Decod).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di TAB_DECOD
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Decodtub.Gruppo_Tab)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Decodtub.Nome_Tab)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Decodtub.Chiave_Tab)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Decodtub.Decodifica_Tab)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Decodtub.Campo1_Tab)));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_EditFormTemplate()
        {
            Tab_EditFormTemplate _tab_EditFormTemplatestub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_EditFormTemplate).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();
            #region Gestione Lista dei Campi del TAB : GENERALI di TAB_DECOD
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_EditFormTemplatestub.Entity_Tab_EditFormTemplate)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_EditFormTemplatestub.Tabs_Tab_EditFormTemplate)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_EditFormTemplatestub.Fields_Tab_EditFormTemplate)));
                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));
                templateDic.Add(generaliTPE, oGeneraleTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Dist()
        {
            Tab_Dist _tab_DistStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Dist).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> generaliTabList = new List<TabPageItemExtended>();

                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_DistStub.Partenza_Tab_Dist)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_DistStub.Arrivo_Tab_Dist)));

                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_DistStub.KM_Tab_Dist)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_DistStub.Minuti_Tab_Dist)));

                templateDic.Add(generaliTPE, generaliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Festivi()
        {
            Tab_Festivi _tab_FestiviStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Festivi).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> generaliTabList = new List<TabPageItemExtended>();
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FestiviStub.Giorno_Tab_Festivi)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FestiviStub.Descrizione_Tab_Festivi)));

                templateDic.Add(generaliTPE, generaliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Funz()
        {
            Tab_Funz _tab_FunzStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Funz).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> generaliTabList = new List<TabPageItemExtended>();
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FunzStub.Nome_Tab_Funz)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FunzStub.Link_Tab_Funz)));

                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FunzStub.Dflt_Grid_Edit_Mode)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_FunzStub.Dflt_OnOffBtnVisible)));

                templateDic.Add(generaliTPE, generaliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Messaggi()
        {
            Tab_Messaggi _tab_MessaggiStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Messaggi).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> generaliTabList = new List<TabPageItemExtended>();
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_MessaggiStub.Tab_Messaggi_Id)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_MessaggiStub.Applicazione_Tab_Messaggi_Id)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_MessaggiStub.Data_Tab_Messaggi)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_MessaggiStub.Testo_Tab_Messaggi)));

                templateDic.Add(generaliTPE, generaliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTabOrari()
        {
            Tab_Orari _tab_OrariStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Orari).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Cant_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Col_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Data_Inizio)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.DisplayedDuration)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Orario_Mensile)));
                //infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Ora_E)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Ora_U)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G1)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G2)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G3)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G4)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G5)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G6)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.G7)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Sequenza)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_OrariStub.Ripetizione)));

                templateDic.Add(generaliTPE, infoTabList);
            }

            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTabOrariTipo()
        {
            Tab_Orari_Tipo _tab_Orari_TipoStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Orari_Tipo).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 3,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Orari_TipoStub.Tab_Orari_Tipo_Entita_Rif)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Orari_TipoStub.Tab_Orari_Tipo_Desc)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Orari_TipoStub.Tab_Orari_Tipo_NotDiu_Auto)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Orari_TipoStub.Tab_Orari_Tipo_Inizio_Not)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_Orari_TipoStub.Tab_Orari_Tipo_Fine_Not)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryTab_Prov()
        {
            Tab_Prov _tab_ProvStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Tab_Prov).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region TAB : Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                //Carica Campi in TAB GENERALI
                List<TabPageItemExtended> generaliTabList = new List<TabPageItemExtended>();
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ProvStub.Sigla_Prov)));
                generaliTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _tab_ProvStub.Descrizione_Prov)));

                templateDic.Add(generaliTPE, generaliTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryUtenti_Fil()
        {
            Utenti_Fil _utenti_FilStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Utenti_Fil).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utenti_FilStub.Fil_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utenti_FilStub.Dominio_Utenti_Fil)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryUtenti_Resp()
        {
            Utenti_Resp _utenti_RespStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Utenti_Resp).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utenti_RespStub.Resp_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utenti_RespStub.Dominio_Utenti_Resp)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryUtenti()
        {
            Utenti _utentiStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Utenti).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Codice_Utente)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Liv_Utente_Edit)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Menu_Tipo_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Versioni_Id)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Lingue_Id)));
                infoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Fil_Inclusive)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Resp_Inclusive)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.SecretQuestion)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.SecretAnswer)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Data_Registrazione_Utente), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.DataOraUltimaModifica_Utente), TabPageItemFieldTypeEnum.NotInGroup));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Password), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Col_Id)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Cli_Id)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.ChangePasswordWarningDays)));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            #region Note
            TabPageExtended noteTPE = new TabPageExtended
            {
                Name = "Note",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_NOTE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, noteTPE.Name))
            {
                List<TabPageItemExtended> noteTabList = new List<TabPageItemExtended>();
                noteTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _utentiStub.Note_Utente), TabPageItemFieldTypeEnum.NotInGroup));
                noteTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                templateDic.Add(noteTPE, noteTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryVersioni()
        {
            Versioni _versioniStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Versioni).Name);

            Dictionary<TabPageExtended, List<TabPageItemExtended>> templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Generali
            TabPageExtended generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                List<TabPageItemExtended> infoTabList = new List<TabPageItemExtended>();
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _versioniStub.Nome_Versioni)));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _versioniStub.DisAbilitazione_Versioni)));

                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _versioniStub.Data_Registrazione_Versioni), TabPageItemFieldTypeEnum.NotInGroup));
                infoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => _versioniStub.DataOraUltimaModifica_Versioni), TabPageItemFieldTypeEnum.NotInGroup));

                templateDic.Add(generaliTPE, infoTabList);
            }
            #endregion

            return templateDic;
        }
        public static Dictionary<TabPageExtended, List<TabPageItemExtended>> GetEditDictionaryAut_Str()
        {
            Aut_Str autStrStub = null;

            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Aut_Str).Name);

            var templateDic = new Dictionary<TabPageExtended, List<TabPageItemExtended>>();

            #region Gestione Lista dei Campi del TAB : GENERALI di FIL

            var generaliTPE = new TabPageExtended
            {
                Name = "Generali",
                Caption = BusinessService.GetLocalizedString(PowerWebResources.STR_GENERALE),
                Columns = 2,
            };
            if (!IsEFTTabDisabled(currentEFT, generaliTPE.Name))
            {
                var oGeneraleTabList = new List<TabPageItemExtended>();
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Col_Id)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Num_Ore)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Data_Inizio)));
                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Data_Fine)));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Resp_Id)));
                oGeneraleTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                oGeneraleTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => autStrStub.Aut_Str_Note), TabPageItemFieldTypeEnum.ColumnSpan, 2));

                templateDic.Add(generaliTPE, oGeneraleTabList);
            }

            #endregion

            return templateDic;
        }

        public static JObject getGridCantForm()
        {

            Cant oCant = null;
            //Spazio vuoto per mantenere l'ordine
            JObject emptySpace = new JObject();
            emptySpace.Add("itemType", "empty");

            //Oggetto che contiene l'intero form
            JObject form = new JObject();
            form.Add("colCount", 1);
            form.Add("showValidationSummary", true);


            JArray items = new JArray();

            JObject tabella = new JObject();
            tabella.Add("itemType", "tabbed");
            JObject tabPanelOptions = new JObject();
            tabPanelOptions.Add("deferRendering", false);
            tabella.Add("tabPanelOptions", tabPanelOptions);
            //Carico i campi dal database
            Tab_EditFormTemplate currentEFT = EditFormTemplates.FirstOrDefault(tmpl => tmpl.Entity_Tab_EditFormTemplate == typeof(Cant).Name);

            //Array contenente i vari tabs
            JArray tabs = new JArray();

            JObject singleTab;

            //Campi contenuti dal singolo tab
            JArray tabFields;

            //Se l'utente ha la versione di assistenza domiciliare, inserisce in testa la tab ASSISTITO
            if (ass_domiciliare_versione != default(Versioni) && PowerWebContext.Current.Versione.Versioni_Id == ass_domiciliare_versione.Versioni_Id)
            {
                #region Gestione Lista dei Campi del TAB : ASSISTITO di CANT

                if (!IsEFTTabDisabled(currentEFT, "Assistito"))
                {
                    singleTab = new JObject();
                    singleTab.Add("title", "Assistito");
                    singleTab.Add("colCount", 2);
                    singleTab.Add("deferRendering", false);
                    tabFields = new JArray();


                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cognome_Assistito_Can), true));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Nome_Assistito_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Livello_Assistito_Can)));

                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Luogo_Nascita_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cap_Nascita_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Provincia_Nascita_Can)));

                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Nascita_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Nascita_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Nascita_Can)));


                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Residenza_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Residenza_Localita_Can)));

                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Residenza_Interno_Can)));
                    //oAssistitoTabList.Add(new TabPageItemExtended(null, TabPageItemFieldTypeEnum.EmptyField));

                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Luogo_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));
                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Nascita_Can)));
                    //oAssistitoTabList.Add(GetEFTField(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Nascita_Can), TabPageItemFieldTypeEnum.NotInGroup));

                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Sesso_Can)));

                    // per l'edit form template di Mosaico questi due campi sono visualizzati nella tab "Generali" e quindi qui non sono visualizzati
                    int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantEditFormTemplateEnum);
                    if (customizationVersion != (int)CantEditFormTemplateEnum.Mosaico)
                    {
                        tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cod_Fisc_Can)));
                        tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Voucher_Can)));
                    }

                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Rapporto_Inizio_1_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Rapporto_Fine_1_Can)));
                    tabFields.Add(emptySpace);

                    singleTab.Add("items", tabFields);
                    tabs.Add(singleTab);
                }
                #endregion
            }

            #region Gestione Lista dei Campi del TAB : GENERALI di CANT
            if (!IsEFTTabDisabled(currentEFT, "generali"))
            {
                singleTab = new JObject();
                singleTab.Add("title", "Generali");
                singleTab.Add("colCount", 2);
                singleTab.Add("deferRendering", false);
                tabFields = new JArray();

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Cantiere)));
                if (ass_domiciliare_versione != default(Versioni) && PowerWebContext.Current.Versione.Versioni_Id == ass_domiciliare_versione.Versioni_Id)
                {
                    tabFields.Add(emptySpace);
                }
                else
                {
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Descrizione_Can)));
                }


                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tipologia_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cli_Id)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Fil_Id)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.DisAbilitazione_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Raggruppamento1_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Raggruppamento2_Can)));


                //per l'edit form template di Mosaico il codice voucher e il codice fiscale sono visualizzati nella tab generali
                int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CantEditFormTemplateEnum);
                if (customizationVersion == (int)CantEditFormTemplateEnum.Mosaico)
                {
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cod_Fisc_Can)));
                    tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Voucher_Can)));
                }

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Data_Registrazione_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.DataOraUltimaModifica_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Singola_Reg)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);

            }
            #endregion

            #region Gestione Lista dei Campi del TAB : UBICAZIONE di CANT

            if (!IsEFTTabDisabled(currentEFT, "Ubicazione"))
            {
                singleTab = new JObject();
                singleTab.Add("title", "Ubicazione");
                singleTab.Add("colCount", 2);
                singleTab.Add("deferRendering", false);

                tabFields = new JArray();

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Provincia_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Luogo_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Cap_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Indirizzo_Can)));

                tabFields.Add(emptySpace);
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Frazione_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Provincia_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Cap_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Luogo_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Domicilio_Luogo_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Localita_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Indirizzo_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Domicilio_Interno_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Nazione_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Zona_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.FlagGps_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.DataVarGps_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.RaggioGps_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.LatitudineGps_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.LongitudineGps_Can)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);

            }
            #endregion

            #region Gestione Lista dei Campi del TAB : TEL/FAX di CANT

            if (!IsEFTTabDisabled(currentEFT, "TelFax"))
            {
                singleTab = new JObject();
                singleTab.Add("title", "TelFax");
                singleTab.Add("colCount", 2);
                singleTab.Add("deferRendering", false);
                tabFields = new JArray();

                tabFields = new JArray();

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_1_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_1_Rif_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_2_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Telefono_2_Rif_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_1_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_1_Rif_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_2_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Fax_2_Rif_Can)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);
            }
            #endregion

            #region Gestione Lista dei Campi del TAB : PARAMETRI di CANT

            singleTab = new JObject();
            singleTab.Add("title", "Parametri");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);
            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Parametri"))
            {

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Min_Ril_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Ril_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Gruppo_Ril_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Max_Gruppo_Notte_Ril_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.TipoNotturno_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Inizio_Notte_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Durata_Notturno_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Numero_GG_Lavorativi)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Cantiere_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Flag_NON_Esportare_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Ore_Massime_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Calcolo_Viaggi_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Interv_Can), true));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Costo_Orario_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tab_Orari_Tipo_Id)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tempo_Attivita_Can)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);
            }

            #endregion

            #region Gestione Lista dei Campi del TAB : ARROTONDAMENTI di CANT

            singleTab = new JObject();
            singleTab.Add("title", "Arrotondamenti");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);
            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Arrotondamenti"))
            {

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Metodo_Arrotondamento_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tipo_Arrotondamento_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Minuti_Tolleranza_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Arrot_Fig_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Minuti_Tolleranza_F_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Arrot_Fig_F_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Arrot_Durata_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Soglia_Durata_Can)));

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Entrata_Mattina_Cant)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Limite_Entrata_Pomeriggio_Cant)));

                tabFields.Add(emptySpace);
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Tolleranza_Limite_Entrata_Pomeriggio_Cant)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);

            }

            #endregion

            #region Gestione Lista dei Campi del TAB : NOTE di CANT
            singleTab = new JObject();
            singleTab.Add("title", "Note");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);
            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Note"))
            {

                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Note_Can)));

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);
            }

            #endregion

            #region Gestione Lista dei Campi del TAB : IMPORTI di CANT
            singleTab = new JObject();
            singleTab.Add("title", "Importi");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);
            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Importi"))
            {
                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);

            }

            #endregion

            #region Gestione Lista dei Campi del TAB : TURNI di CANT

            singleTab = new JObject();
            singleTab.Add("title", "Turni");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);
            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Turni"))
            {


                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);
            }
            #endregion

            #region Gestione Lista dei Campi del TAB : SERVIZI di CANT

            singleTab = new JObject();
            singleTab.Add("title", "Servizi");
            singleTab.Add("colCount", 2);
            singleTab.Add("deferRendering", false);

            tabFields = new JArray();

            if (!IsEFTTabDisabled(currentEFT, "Servizi"))
            {


                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Commessa_Can)));
                tabFields.Add(GetEFTFieldJson(currentEFT, CommonService.GetPropertyName(() => oCant.Codice_Gestionale_Can)));
                tabFields.Add(emptySpace);

                singleTab.Add("items", tabFields);
                tabs.Add(singleTab);

            }

            #endregion


            //Aggiungo il tutto all'oggetto form
            tabella.Add("tabs", tabs);
            items.Add(tabella);
            form.Add("items", items);

            return form;
        }
    }
}



