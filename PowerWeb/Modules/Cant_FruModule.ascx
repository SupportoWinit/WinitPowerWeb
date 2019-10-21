<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Cant_FruModule.ascx.cs"
    Inherits="PowerWeb.Modules.Cant_FruModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<script type="text/javascript">

    function OnNSerieFruChanged(cmbNSerieFru) {

        var grid = ASPxClientGridView.Cast(detailGrid);
        var Fru_IdCmb = ASPxClientComboBox.Cast(grid.GetEditor("Fru_Id"));
        Fru_IdCmb.ClearItems();

        var codeFru = cmbNSerieFru.GetSelectedItem().GetColumnText("Codice_Fru");
        var isDisable = cmbNSerieFru.GetSelectedItem().GetColumnText("DisAbilitazione_Fru");
        var nSerie = cmbNSerieFru.GetSelectedItem().GetColumnText("N_Serie_Fru");
        var fullItem = new Array(codeFru, isDisable, nSerie);
        //Fru_IdCmb.AddItem(fullItem, cmbNSerieFru.GetValue());
        Fru_IdCmb.AddItem(fullItem, cmbNSerieFru.GetSelectedItem().GetColumnText("Fru_Id"));
        Fru_IdCmb.SetSelectedIndex(0);
    }

    function OnCodice_FruChanged(cmbFru) {
        var grid = ASPxClientGridView.Cast(detailGrid);
        var N_Serie_FruCmb = ASPxClientComboBox.Cast(grid.GetEditor("N_Serie_Fru"));
        N_Serie_FruCmb.SetValue(cmbFru.GetSelectedItem().GetColumnText("N_Serie_Fru"));
    }

</script>

<dx:ASPxGridView ID="gvCant_Fru" runat="server" AutoGenerateColumns="False" Width="100%" OnDetailRowExpandedChanged="gvCant_Fru_DetailRowExpandedChanged" OnDataBinding="gvCant_Fru_DataBinding">
    <Columns>
        <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Can" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia1_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia2_Can" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="True" Settings-AllowHeaderFilter="False">
            <Settings AllowHeaderFilter="False" />
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cap_Can" Visible="False">
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
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_1_Can" Visible="False">
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_2_Can" Visible="False">
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_3_Can" Visible="False">
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_4_Can" Visible="False">
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_5_Can" Visible="False">
            <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
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
        <dx:GridViewDataTextColumn FieldName="Indirizzo_Can" VisibleIndex="70" Width="25%">
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
        <dx:GridViewDataTextColumn FieldName="N_Fru_Cant" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Nr_Isee_Can" Visible="False">
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
        <dx:GridViewDataComboBoxColumn FieldName="Tipologia_Can" VisibleIndex="10" Width="7%">
        </dx:GridViewDataComboBoxColumn>
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
            <dx:ASPxGridView ID="gvCant_Fru_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvCant_Fru_Detail_Init"
                OnInitNewRow="gvCant_Fru_Detail_InitNewRow"
                OnRowValidating="gvCant_Fru_Detail_RowValidating"
                OnRowInserting="gvCant_Fru_Detail_RowInserting"
                OnRowUpdating="gvCant_Fru_Detail_RowUpdating"
                OnRowDeleting="gvCant_Fru_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvCant_Fru_Detail_BeforePerformDataSelect">
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
                        <%--                      <EditButton Visible="True">
                          <Image Url="../Icons/Edit/Edit.png"/>
                      </EditButton>--%>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Undo/Undo.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Fru_Id" VisibleIndex="10" Width="10%">
                        <Settings AllowHeaderFilter="False" />
                        <PropertiesComboBox EnableSynchronization="False">
                            <ClientSideEvents SelectedIndexChanged="function(s, e) { OnCodice_FruChanged(s); }" />
                        </PropertiesComboBox>
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="N_Serie_Fru" VisibleIndex="15" Width="10%" ReadOnly="False">
                        <PropertiesComboBox EnableSynchronization="False">
                            <ClientSideEvents SelectedIndexChanged="function(s, e) { OnNSerieFruChanged(s); }"></ClientSideEvents>
                        </PropertiesComboBox>
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataDateColumn FieldName="Abilitazione_Data_Inizio_Fru_Can" VisibleIndex="20" Width="15%">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Fru_Can" Visible="False" ReadOnly="True">
                        <PropertiesDateEdit EditFormat="DateTime" />
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Fru_Can" Visible="False" ReadOnly="true">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Fru_Can" VisibleIndex="100" Width="5%">
                    </dx:GridViewDataCheckColumn>
                    <dx:GridViewDataTextColumn FieldName="Note_Fru_Can" VisibleIndex="30" Width="50%">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
