<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Cant_NoteModule.ascx.cs"
    Inherits="PowerWeb.Modules.Cant_NoteModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<dx:ASPxGridView ID="gvCant_Note" runat="server" AutoGenerateColumns="False" Width="100%" OnDetailRowExpandedChanged="gvCant_Note_DetailRowExpandedChanged">
    <Columns>
        <dx:GridViewDataTextColumn FieldName="Cant_Id" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia1_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia2_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cap_Can" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cap_Nascita_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cli_Id" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Cod_Fisc_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Cantiere" VisibleIndex="20" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Luogo_Nascita_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Luogo_Residenza_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Voucher_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Cognome_Assistito_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CognomeNome_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Contributo_Disabili_Fascia1_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Contributo_Disabili_Fascia2_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Costo_Mezzora_Prescuola_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Costo_Orario_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Isee_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Nascita_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_1_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_2_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_3_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_4_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_5_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_1_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_2_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_3_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_4_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_5_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Can" Visible="False" ReadOnly="true">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Can" Visible="False" ReadOnly="true">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataVarGps_Can" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataTextColumn FieldName="Descrizione_Can" VisibleIndex="30" Width="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Can" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Gruppo_Notte_Ril_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Gruppo_Ril_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Ril_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Min_Ril_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Fil_Id" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Mattina_Col" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="FlagGps_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Gestione_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Indirizzo_Can" VisibleIndex="70" Width="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="LatitudineGps_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Limite_Inizio_Notte_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Pomeriggio_Col" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Livello_Assistito_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="LongitudineGps_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Luogo_Can" VisibleIndex="60" Width="20%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Luogo_Nascita_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Mensa_Can" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Metodo_Arrotondamento_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tolleranza_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tolleranza_F_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nazione_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nazione_Nascita_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Nome_Assistito_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Nr_Isee_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Note_Cant" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Bimbi_Fascia_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_GG_Lavorativi" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia1_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia2_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PreScuola_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_1Mezzora_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_2Mezzora_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Ore_Massime_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Can" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="P" DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_1_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_2_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_3_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_4_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_5_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Provincia_Can" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Provincia_Nascita_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="RaggioGps_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="Raggruppamento1_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Raggruppamento2_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Sesso_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Singola_Reg" VisibleIndex="90" Width="7%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_F_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Soglia_Durata_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Rif_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Arrotondamento_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Calcolo_Viaggi_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Cantiere_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Tipo_Interv_Can" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_1_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_2_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_3_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_4_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_5_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Tipologia_Can" VisibleIndex="10" Width="7%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="TipoNotturno_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno1_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno2_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno3_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno4_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno5_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno6_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno7_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno8_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno9_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Turno10_Can" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Zona_Can" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Entrata_Pomeriggio_Col" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvCant_Note_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvCant_Note_Detail_Init"
                OnInitNewRow="gvCant_Note_Detail_InitNewRow"
                OnRowValidating="gvCant_Note_Detail_RowValidating"
                OnRowInserting="gvCant_Note_Detail_RowInserting"
                OnRowUpdating="gvCant_Note_Detail_RowUpdating"
                OnRowDeleting="gvCant_Note_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvCant_Note_Detail_BeforePerformDataSelect">
                <Columns>
                    <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
                        <CustomButtons>
                            <dx:GridViewCommandColumnCustomButton ID="add">
                                <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="addClone">
                                <Image ToolTip="AddClone" Url="../Icons/Add/Add.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="delete">
                                <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="view">
                                <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                            </dx:GridViewCommandColumnCustomButton>
                        </CustomButtons>
                        <EditButton Visible="True">
                            <Image Url="../Icons/Edit/Edit.png" />
                        </EditButton>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Undo/Undo.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataTextColumn FieldName="Cant_Note_Id" Visible="false" ReadOnly="true">
                    </dx:GridViewDataTextColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="false" ReadOnly="true">
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Nota_Can_Note" VisibleIndex="20" Width="5%">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Can_Note" Visible="False" ReadOnly="true">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Can_Note" Visible="False" ReadOnly="true">
                        <PropertiesDateEdit EditFormat="DateTime" />
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Can_Note" VisibleIndex="110" Width="5%">
                    </dx:GridViewDataCheckColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Utenti_Id" VisibleIndex="30" Width="10%">
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataTextColumn FieldName="Nota_Can_Note" VisibleIndex="40" Width="60%">
                    </dx:GridViewDataTextColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Nota_Can_Note" VisibleIndex="10" Width="10%">
                    </dx:GridViewDataComboBoxColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
