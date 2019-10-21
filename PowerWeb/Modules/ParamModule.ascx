<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ParamModule.ascx.cs"
    Inherits="PowerWeb.Modules.ParamModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxTimer" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<script type="text/javascript">

    //#region -------------- Gestione barra di progressione cambio data archiviazione -------------------

    function deNewStoredRegDate_OnDateChanged(s, e) {
        var currentDE = ASPxClientDateEdit.Cast(s);
        btnConfirmStoredRegDate.SetEnabled(currentDE.GetDate() != null);
    }

    function btnConfirmStoredRegDate_OnClick(s, e) {
        DisplayJConfirm("Power", btnConfirmStoredRegDate.cpConfirmMessage, function (r) {
            if (r) {
                // blocco del pulsante e avvio del cambio data su server
                btnConfirmStoredRegDate.SetEnabled(false);
                cElChangeStoredRegDate.PerformCallback();
                tElChangeStoredRegDatePing.SetEnabled(true);
            }
        });
    }

    function cElChangeStoredRegDate_OnCallbackComplete(s, e) {
        tElChangeStoredRegDatePing.SetEnabled(false);
        storedRegDateProgressBar.SetPosition(100);

        var callbackResult = e.result.split('|');
        if (callbackResult[0] == 'Error') {
            DisplayDialogError(callbackResult[1], callbackResult[2]);
        } else {
            storedRegDateProgressLabel.SetText(callbackResult[1]);
        }
        btnConfirmStoredRegDate.SetEnabled(false);
    }

    function tElChangeStoredRegDatePing_OnTick(s, e) {
        cElChangeStoredRegDatePing.PerformCallback();
    }

    function cElChangeStoredRegDatePing_OnCallbackComplete(s, e) {
        var progrAndCommandArray = e.result.split('|');
        var currentPB = ASPxClientProgressBar.Cast(storedRegDateProgressBar);
        var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
        currentPB.SetPosition(currentProgress);
        storedRegDateProgressLabel.SetText(progrAndCommandArray[1]);
    }

    function cConfirmStoredRegDate_OnCallbackComplete(s, e) {
        deOldStoredRegDate.SetDate(deNewBreakRegDate.GetDate());
        deNewStoredRegDate.SetDate(null);
        btnConfirmStoredRegDate.SetEnabled(false);
    }

    function tElChangeBreakRegDatePing_OnTick(s, e) {
        cElChangeStoredRegDatePing.PerformCallback();
    }

    //#endregion

    //#region --------------Gestione Data Blocco -------------------------------------
    function deNewBreakRegDate_OnDateChanged(s, e) {
        var currentDE = ASPxClientDateEdit.Cast(s);
        btnConfirmBreakRegDate.SetEnabled(currentDE.GetDate() != null);
    }

    function btnConfirmBreakRegDate_OnClick(s, e) {
        DisplayJConfirm("Power", btnConfirmBreakRegDate.cpConfirmMessage, function (r) {
            if (r) {
                // blocco del pulsante e avvio del cambio data su server
                btnConfirmBreakRegDate.SetEnabled(false);
                cElChangeBreakRegDate.PerformCallback();
                tElChangeBreakRegDatePing.SetEnabled(true);
            }
        });
    }

    function cElChangeBreakRegDate_OnCallbackComplete(s, e) {
        tElChangeBreakRegDatePing.SetEnabled(false);
        breakRegDateProgress.SetPosition(100);

        var callbackResult = e.result.split('|');
        if (callbackResult[0] == 'Error') {
            DisplayDialogError(callbackResult[1], callbackResult[2]);
        } else {
            breakRegDateProgressLabel.SetText(callbackResult[1]);
        }
        btnConfirmBreakRegDate.SetEnabled(false);
    }

    function tElChangeBreakRegDatePing_OnTick(s, e) {
        cElChangeBreakRegDatePing.PerformCallback();
    }

    function cElChangeBreakRegDatePing_OnCallbackComplete(s, e) {
        var progrAndCommandArray = e.result.split('|');
        var currentPB = ASPxClientProgressBar.Cast(breakRegDateProgress);
        var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
        currentPB.SetPosition(currentProgress);
        breakRegDateProgressLabel.SetText(progrAndCommandArray[1]);
    }

    function cConfirmBreakRegDate_OnCallbackComplete(s, e) {
        deOldBreakRegDate.SetDate(deNewBreakRegDate.GetDate());
        deNewBreakRegDate.SetDate(null);
        btnConfirmBreakRegDate.SetEnabled(false);
    }
    //#endregion
</script>
<dx:ASPxGridView ID="gvParam" runat="server" AutoGenerateColumns="False" Width="100%"
    OnRowValidating="gvParam_RowValidating"
    OnRowUpdating="gvParam_RowUpdating">
    <Columns>
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
            <CustomButtons>
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
        <dx:GridViewDataTextColumn FieldName="Param_Id" Visible="False" ReadOnly="true">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Cliente" Visible="False" ReadOnly="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="ActivationDate" Visible="False" ReadOnly="true">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Arrotondamenti" Visible="False" ReadOnly="True">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Att" Visible="False" ReadOnly="True">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Cartellino" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Confronto_Ore_Budget" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_GPS" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Monte_Minuti" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Notturno" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Orari" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Pass" VisibleIndex="50" Width="5%" ReadOnly="True">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Privacy" VisibleIndex="51" Width="5%" ReadOnly="True">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Schedulatore" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Viaggi" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Where_Is_It" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Aut_Str" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Notifiche" Visible="False" ReadOnly="true">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Attiva_Num_Aut_Can" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Attiva_Num_Aut_Col" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="BingKey" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="ComboBoxDelay" Visible="false" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="ComboboxRowsPerPage" Visible="false" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="CompanyAddress" VisibleIndex="20" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CompanyInfo" VisibleIndex="30" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CompanyName" VisibleIndex="10" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CompanyEmail" VisibleIndex="40" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Ctrl_Tab_Comuni" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Ctrl_Codice_Fisc" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Ctrl_Codice_IBAN" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Ctrl_Sovrap_Pass" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione" Visible="false">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica" Visible="false">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Default_Abilitati" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Durata_Giornata" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Durata_Max_Gruppo_Notte_Ril" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Durata_Max_Gruppo_Ril" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Durata_Max_Ril" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Durata_Min_Ril" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Minuti_Arrot_I" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Minuti_Arrot_F" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Minuti_Durata" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="Default_Ora_Inizio_Giornata" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Soglia_Arrot_I" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Soglia_Arrot_F" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Default_Soglia_Durata" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataCheckColumn FieldName="Dflt_DoRefresh_Grid" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Dflt_Include_Activities" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Dflt_Include_Pass" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Dflt_Include_Trips" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Dflt_Include_Blocked" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Sabato_Feriale" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="DomainFilter" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Durata_Massima_Viaggio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Durata_Minima_Viaggio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Durata_Pausa" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_1_Inizio" Visible="false">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_1_Fine" Visible="false">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_2_Inizio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_2_Fine" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_3_Inizio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_3_Fine" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_4_Inizio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_4_Fine" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_5_Inizio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_5_Fine" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Flag_Calcolo_Viaggi" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_GPS" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_Monte_Ore" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_Ore_Viaggi" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Flag_Ore_Viaggi_Inizio_Fine" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="File_Cant_Var" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="File_Col_Var" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataSpinEditColumn FieldName="GG_X_Allarmi" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="MaxElab_ImportChunkSize" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="MDBPath" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Mesi_Validita_Reg" VisibleIndex="60" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Metodo_Arrotondamento" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="ModuleType" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Detrazione_Pausa" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="PercorsoFotoCol" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="RaggioGpsDefault" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="RowsPerComboBox" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="RowsPerPage" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Tempo_Doppia_Reg" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Arrotondamento" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Assegnazione_KMMinuti" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Assegnazione_KMMinuti_Inizio_Fine_G" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="TipoNotturno" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Viaggio" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Utilizzo_Fasce_Viaggi" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Ass_Tag_Gps" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tipo_Chiusura_Causali" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Importazione_timbrature_GPS" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Utilizzo_Limite_Entrata" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Mattina" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Pomeriggio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Entrata_Pomeriggio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Inizio_Pomeriggio" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Limite_Entrata_Usa_Orario" Visible="False" />
        <dx:GridViewDataComboBoxColumn FieldName="Aut_Str_Tipo" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cantiere_Timbrature_GPS_Non_Valide" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Durata_Notturno" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Cartellino_Inizio_Notturno" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Cartellino_Fine_Notturno" Visible="False">
            <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Divisione_Piano_Notturno_Diurno" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Piano" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Ore" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Motivazioni" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Delta" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Totale" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Viaggi" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Visualizza_Totali_Settimanali" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Modalita_Compatta" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Usa_Cartellino_Modificabile" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Totale_Prima_Colonna" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Abilita_Divisione_Cantiere" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Usa_Rettifiche_Auto" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Usa_Rettifiche_Manuali" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Abilita_Stampa_Cart_Editabile" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Durata_Quadratura_Rettifiche_Auto" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Cartellino_Abilita_Modifica" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Ritardo_Tolleranza_Minuti" Visible="False">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataCheckColumn FieldName="Sincronizzazione_ClockApp" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="Url_ClockAppsManager" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="ImportaFileTimbratureSospese" Visible="False">
        </dx:GridViewDataCheckColumn>
         <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Mattina" Visible="False">
             <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
         <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Pomeriggio" Visible="False">
             <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
         <dx:GridViewDataCheckColumn FieldName="Limite_Uscita_Usa_Orario" Visible="False">
        </dx:GridViewDataCheckColumn>
         <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Uscita_Mattina" Visible="False">
             <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
         <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Uscita_Pomeriggio" Visible="False">
             <PropertiesTextEdit>
                <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                <MaskSettings Mask="00:00" IncludeLiterals="None" />
            </PropertiesTextEdit>
        </dx:GridViewDataTextColumn>
         <dx:GridViewDataComboBoxColumn FieldName="Utilizzo_Limite_Uscita" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Sincronizzazione_Entità" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Abilita_Import_Esterno" Visible="False">
        </dx:GridViewDataCheckColumn>
         <dx:GridViewDataTextColumn FieldName="Soglia_Minima_Arrotondamento_Durata" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="BlockLoginOnUserPswExpired" Visible="False">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataSpinEditColumn FieldName="PasswordExpirationDays" Visible="False">
        </dx:GridViewDataSpinEditColumn>
    </Columns>
</dx:ASPxGridView>
<br />
<dx:ASPxFormLayout ID="flChangeBreakRegDate" runat="server" Width="80%">
    <Items>
        <dx:LayoutGroup Caption="Cambio data Blocco" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <div style="float: left">
                                <table>
                                    <tr>
                                        <td>
                                            <dx:ASPxLabel ID="lblOldBreakRegDate" runat="server" AssociatedControlID="deOldBreakRegDate" />
                                            <dx:ASPxDateEdit ID="deOldBreakRegDate" ClientInstanceName="deOldBreakRegDate" runat="server" ReadOnly="true" Enabled="false"></dx:ASPxDateEdit>
                                        </td>
                                        <td>
                                            <dx:ASPxLabel ID="lblNewBreakRegDate" runat="server" AssociatedControlID="deNewBreakRegDate" />
                                            <dx:ASPxDateEdit ID="deNewBreakRegDate" runat="server" ClientInstanceName="deNewBreakRegDate">
                                                <ClientSideEvents DateChanged="deNewBreakRegDate_OnDateChanged" />
                                            </dx:ASPxDateEdit>
                                        </td>
                                        <td>
                                            <dx:ASPxButton ID="btnConfirmBreakRegDate" runat="server" ClientInstanceName="btnConfirmBreakRegDate" ClientEnabled="false" OnCustomJSProperties="btnConfirmBreakRegDate_CustomJSProperties" AutoPostBack="false" UseSubmitBehavior="false">
                                                <ClientSideEvents Click="btnConfirmBreakRegDate_OnClick" />
                                            </dx:ASPxButton>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
                <dx:LayoutItem Caption="Avanzamento" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="vertical-align: top">
                                        <dx:ASPxProgressBar ID="BreakRegDateProgressBar" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="breakRegDateProgress">
                                        </dx:ASPxProgressBar>
                                    </td>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxLabel ID="BreakRegDateProgressLabel" runat="server" Width="100%" ClientInstanceName="breakRegDateProgressLabel">
                                        </dx:ASPxLabel>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>

<dx:ASPxFormLayout ID="flChangeStoreDate" runat="server" Width="80%">
    <Items>
        <dx:LayoutGroup Caption="Cambio Data Archiviazione" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <div style="float: left">
                                <table>
                                    <tr>
                                        <td>
                                            <dx:ASPxLabel ID="lblOldStoredRegDate" runat="server" AssociatedControlID="deOldStoredRegDate" />
                                            <dx:ASPxDateEdit ID="deOldStoredRegDate" ClientInstanceName="deOldStoredRegDate" runat="server" ReadOnly="true" Enabled="false"></dx:ASPxDateEdit>
                                        </td>
                                        <td>
                                            <dx:ASPxLabel ID="lblNewStoredRegDate" runat="server" AssociatedControlID="deNewStoredRegDate" />
                                            <dx:ASPxDateEdit ID="deNewStoredRegDate" runat="server" ClientInstanceName="deNewStoredRegDate">
                                                <ClientSideEvents DateChanged="deNewStoredRegDate_OnDateChanged" />
                                            </dx:ASPxDateEdit>
                                        </td>
                                        <td>
                                            <dx:ASPxButton ID="btnConfirmStoredRegDate" runat="server" ClientInstanceName="btnConfirmStoredRegDate" ClientEnabled="false" OnCustomJSProperties="btnConfirmStoredRegDate_CustomJSProperties" AutoPostBack="false" UseSubmitBehavior="false">
                                                <ClientSideEvents Click="btnConfirmStoredRegDate_OnClick" />
                                            </dx:ASPxButton>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
                <dx:LayoutItem Caption="Avanzamento" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="vertical-align: top">
                                        <dx:ASPxProgressBar ID="StoredRegDateProgressBar" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="storedRegDateProgressBar">
                                        </dx:ASPxProgressBar>
                                    </td>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxLabel ID="StoredRegDateProgressLabel" runat="server" Width="100%" ClientInstanceName="storedRegDateProgressLabel">
                                        </dx:ASPxLabel>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>


<div>
    <dx:ASPxCallback ID="cConfirmBreakRegDate" ClientInstanceName="cConfirmBreakRegDate" runat="server">
        <ClientSideEvents CallbackComplete="cConfirmBreakRegDate_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cElChangeBreakRegDate" ClientInstanceName="cElChangeBreakRegDate" runat="server" OnCallback="cElChangeBreakRegDate_Callback">
        <ClientSideEvents CallbackComplete="cElChangeBreakRegDate_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cElChangeBreakRegDatePing" ClientInstanceName="cElChangeBreakRegDatePing" runat="server" OnCallback="cElChangeBreakRegDatePing_Callback">
        <ClientSideEvents CallbackComplete="cElChangeBreakRegDatePing_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxTimer ID="tElChangeBreakRegDatePing" ClientInstanceName="tElChangeBreakRegDatePing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tElChangeBreakRegDatePing_OnTick" />
    </dx:ASPxTimer>

    <dx:ASPxCallback ID="cConfirmStoredRegDate" ClientInstanceName="cConfirmStoredRegDate" runat="server">
        <ClientSideEvents CallbackComplete="cConfirmStoredRegDate_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cElChangeStoredRegDate" ClientInstanceName="cElChangeStoredRegDate" runat="server" OnCallback="cElChangeStoredRegDate_Callback">
        <ClientSideEvents CallbackComplete="cElChangeStoredRegDate_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cElChangeStoredRegDatePing" ClientInstanceName="cElChangeStoredRegDatePing" runat="server" OnCallback="cElChangeStoredRegDatePing_Callback">
        <ClientSideEvents CallbackComplete="cElChangeStoredRegDatePing_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxTimer ID="tElChangeStoredRegDatePing" ClientInstanceName="tElChangeStoredRegDatePing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tElChangeStoredRegDatePing_OnTick" />
    </dx:ASPxTimer>
</div>
