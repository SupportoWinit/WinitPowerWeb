<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Col_PruModule.ascx.cs"
    Inherits="PowerWeb.Modules.Col_PruModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<script type="text/javascript">

    //CASCADE quando cambio il numero di serie ----> mi cambia anche il codice Pru di conseguenza
    function OnNSeriePruChanged(cmbNSeriePru) {

        var grid = ASPxClientGridView.Cast(detailGrid);
        var Pru_IdCmb = ASPxClientComboBox.Cast(grid.GetEditor("Pru_Id"));
        Pru_IdCmb.ClearItems();

        //STESSO PROCEDIMENTO VALE PER IL MODULO Cant_FrueModule
        //mi creo delle variabili inserendo in base al numero di serie di Pru che vado a selez nella combo
        //inserisco in base al numero di serie la matricola della Pru
        var codePru = cmbNSeriePru.GetSelectedItem().GetColumnText("Codice_Pru");
        //inserisco la data di disabilitazione
        var isDisable = cmbNSeriePru.GetSelectedItem().GetColumnText("DisAbilitazione_Pru");
        //inserisco il numero di serire
        var nSerie = cmbNSeriePru.GetSelectedItem().GetColumnText("N_Serie_Pru");
        //mi creo un array con i calori che dericano dalla selezione del numero di serie
        var fullItem = new Array(codePru, isDisable, nSerie);
        //vado ad aggiornare la combobox del codice Pru in base ai valori dell'array
        Pru_IdCmb.AddItem(fullItem, cmbNSeriePru.GetValue());
        //i nuovi valori inseriti li inserisco in cima alla combo
        Pru_IdCmb.SetSelectedIndex(0);
    }

    //CASCADE quando cambio il codice_Pru ----> cambia il numero di serie di conseguenza
    function OnCodice_PruChanged(cmbPru) {



        var grid = ASPxClientGridView.Cast(detailGrid);
        var N_Serie_PruCmb = ASPxClientComboBox.Cast(grid.GetEditor("N_Serie_Pru"));
        N_Serie_PruCmb.SetValue(cmbPru.GetSelectedItem().GetColumnText("N_Serie_Pru"));
    }



</script>
<dx:ASPxGridView ID="gvCol_Pru" runat="server" AutoGenerateColumns="False" Width="100%" OnDetailRowExpandedChanged="gvCol_Pru_DetailRowExpandedChanged" OnDataBinding="gvCol_Pru_DataBinding">
    <Columns>
        <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="ArrotF_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="ArrotI_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataCheckColumn FieldName="Assegni_Famigliari_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataDateColumn FieldName="Assegni_Famigliari_Fine_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Assegni_Famigliari_Inizio_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Automunito_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="false">
            <Settings AllowHeaderFilter="False" />
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Collaboratore" VisibleIndex="10" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Codice_Domicilio_Luogo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Fiscale_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Iban_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Codice_Nascita_Luogo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Codice_Residenza_Luogo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Cognome_Col" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CognomeNome_Col" VisibleIndex="20" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Fine_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Inizio_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Col" Visible="false" ReadOnly="True">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Sorv_San_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Col" Visible="false" ReadOnly="True">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Disabile_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Col" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Col" VisibleIndex="40" Width="5%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Domicilio_Interno_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Domicilio_Localita_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Col" VisibleIndex="50" Width="15%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Col" VisibleIndex="30" Width="5%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Gruppo_Notte_Ril_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Gruppo_Ril_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Max_Ril_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Min_Ril_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Durata_Pausa_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_1_Inizio_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_1_Fine_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_2_Inizio_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_2_Fine_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_3_Inizio_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_3_Fine_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_4_Inizio_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_4_Fine_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_5_Inizio_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Fascia_Ore_Viaggi_5_Fine_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Mattina_Col" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_INPS_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_Ore_Viaggi_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="GGConsMax_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Indennita_Sanificazione_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Indennita_Trasporto_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="Libretto_Sanitario_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Pomeriggio_Col" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTimeEditColumn FieldName="Limite_Inizio_Notte_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Livello_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Matricola_Col" VisibleIndex="80" Width="7%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Metodo_Arrotondamento_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Minuti_Arrot_Durata_Fig_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="N_Persone_A_Carico_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="N_Pos_INAIL_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Pos_INPS_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Pru_Col" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nascita_Cap_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataDateColumn FieldName="Nascita_Data_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nascita_Luogo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nascita_Provincia_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Nazionalita_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Nome_Col" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTimeEditColumn FieldName="OreMaxGG_Col" Visible="False">
        </dx:GridViewDataTimeEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="OreMassime" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Patente_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Prova" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Qualifica_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Quota_PTime_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento2_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Cap_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Indirizzo_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Luogo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_GEN_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Resp_Id" VisibleIndex="90" Width="10%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Netta_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Lorda_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Oraria_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Straordinaria_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataDateColumn FieldName="Scadenza_Patente_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Sesso_Col" VisibleIndex="60" Width="5%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Singola_Reg" VisibleIndex="90" Width="7%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Durata_Fig_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Soglia_Durata_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g">
            </PropertiesSpinEdit>
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="SogliaF_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="SogliaI_Col" Visible="False">
            <PropertiesSpinEdit DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Stato_Civile_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Straniero_CEE_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Straniero_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataDateColumn FieldName="Straniero_Scadenza_Permesso_Col" Visible="False">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Id" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Rif_Col" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Arrotondamento_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Rapporto_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Contratto_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Titolo_Studio_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="TipoNotturno_Col" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Trattenuta_Vitto_Col" Visible="False">
            <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Zona_Col" Visible="False">
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
            <dx:ASPxGridView ID="gvCol_Pru_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvCol_Pru_Detail_Init"
                OnInitNewRow="gvCol_Pru_Detail_InitNewRow"
                OnRowValidating="gvCol_Pru_Detail_RowValidating"
                OnRowInserting="gvCol_Pru_Detail_RowInserting"
                OnRowDeleting="gvCol_Pru_Detail_RowDeleting"
                OnRowUpdating="gvCol_Pru_Detail_RowUpdating"
                OnBeforePerformDataSelect="gvCol_Pru_Detail_BeforePerformDataSelect">
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
                        <%--<EditButton Visible="True">
                            <Image Url="../Icons/Edit/Edit.png" />
                        </EditButton>--%>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Undo/Undo.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>

                    <dx:GridViewDataComboBoxColumn FieldName="Pru_Id" VisibleIndex="10" Width="10%">
                        <Settings AllowHeaderFilter="False" />
                        <PropertiesComboBox EnableSynchronization="False">
                            <ClientSideEvents SelectedIndexChanged="function(s, e) { OnCodice_PruChanged(s); }" />
                        </PropertiesComboBox>
                    </dx:GridViewDataComboBoxColumn>

                    <dx:GridViewDataComboBoxColumn FieldName="N_Serie_Pru" VisibleIndex="15" Width="10%">
                        <PropertiesComboBox EnableSynchronization="False">
                            <ClientSideEvents SelectedIndexChanged="function(s, e) { OnNSeriePruChanged(s); }" />
                        </PropertiesComboBox>
                    </dx:GridViewDataComboBoxColumn>

                    <dx:GridViewDataDateColumn FieldName="Abilitazione_Data_Inizio_Pru_Col" VisibleIndex="5" Width="15%">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Pru_Col" Visible="false" ReadOnly="true">
                        <PropertiesDateEdit EditFormat="DateTime" />
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Pru_Col" Visible="false" ReadOnly="true">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Pru_Col" VisibleIndex="100" Width="15%">
                    </dx:GridViewDataCheckColumn>
                    <dx:GridViewDataTextColumn FieldName="Note_Pru_Col" VisibleIndex="70" Width="50%">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
