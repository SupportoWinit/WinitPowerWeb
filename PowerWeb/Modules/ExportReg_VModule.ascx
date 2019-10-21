<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ExportReg_VModule.ascx.cs" Inherits="PowerWeb.Modules.ExportReg_VModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxFormLayout" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallback" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallbackPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>

<style type="text/css">
    .noDataSource {
        display: none;
    }
</style>

<script type="text/javascript">

    // ----------------------------------- gestione della selezione dei collaboratori in griglia ---------------------------------------
    var _selectNumberCol = 0;
    var _selectNumberOverPageCol = 0;
    var selectionTypeCol = "none";
    var _handleCol = true;

    function OnGridColSelectionChanged(s, e) {

        cbAllCol.SetChecked(s.GetSelectedRowCount() == s.cpVisibleRowCount);

        if (e.isChangedOnServer == false) {
            if (e.isAllRecordsOnPage && e.isSelected) {
                _selectNumberOverPageCol = s.GetVisibleRowsOnPage();
                _selectNumberCol = _selectNumberCol + s.GetVisibleRowsOnPage(); // when all rows are selected within the page
            }
            else if (e.isAllRecordsOnPage && !e.isSelected) {
                _selectNumberCol = _selectNumberCol - s.GetVisibleRowsOnPage(); // when all rows are deselected within the page
                if (_selectNumberCol == 0)
                    _selectNumberOverPageCol = 0;
            }
            else if (!e.isAllRecordsOnPage && e.isSelected) {
                selectionTypeCol = "sRow#Col";

                _selectNumberCol++; // when one row is selected
                _selectNumberOverPageCol++;
            }
            else if (!e.isAllRecordsOnPage && !e.isSelected) {
                selectionTypeCol = "uRow#Col";

                _selectNumberCol--; // when one row is deselected
                _selectNumberOverPageCol--;
            }

            if (_handleCol) { // if the selection wasn’t performed by clicking the cbPageCol
                cbPageCol.SetChecked(_selectNumberOverPageCol == s.GetVisibleRowsOnPage()); // let’s change the cbPageCol state if needed
                _handleCol = false;
            }

            _handleCol = true;
        }
        else {
            cbPageCol.SetChecked(cbAllCol.GetChecked()); // if the selection was performed on the server, let’s check cbPageCol
        }

        gridMasterSelectionChange.PerformCallback(selectionTypeCol + '#' + s.GetSelectedRowCount());
    }

    function OnGridColEndCallback(s, e) {
        if (grid.cpPageChanged == 1) {
            _selectNumberOverPageCol = 0;
        }

        if (selectionTypeCol == 'sAll#Col')
            _selectNumberCol = s.cpVisibleRowCount;
        else if (selectionTypeCol == 'uAll#Col') {
            _selectNumberCol = 0;
        }
    }

    function OnAllColCheckedChanged(s, e) {
        if (s.GetChecked()) {
            DisplayJConfirm('Power', s.cpMessage, function (r) {
                if (r) {
                    grid.SelectRows();
                    selectionTypeCol = "sAll#Col";
                }
                else {
                    s.SetChecked(false);
                    selectionTypeCol = "uAll#Col";
                    grid.UnselectRows();
                }
            });
        }
        else {
            s.SetChecked(false);
            selectionTypeCol = "uAll#Col";
            grid.UnselectRows();
        }

        // gestione dell'abilitazione/disabilitazione del tasto di lancio export
        EnableOrDisableBtnExport();
    }

    function OnPageColCheckedChanged(s, e) {
        _handleCol = false;

        if (s.GetChecked()) {
            selectionTypeCol = "sPage#Col";
            grid.SelectAllRowsOnPage();
        }

        else {
            selectionTypeCol = "uPage#Col";
            grid.UnselectAllRowsOnPage();
        }

        // gestione dell'abilitazione/disabilitazione del tasto di lancio export
        EnableOrDisableBtnExport();
    }

    // ----------------------------------- gestione della selezione dei cantieri in griglia ---------------------------------------
    var _selectNumberCant = 0;
    var _selectNumberOverPageCant = 0;
    var selectionTypeCant = "none";
    var _handleCant = true;

    function OnGridCantSelectionChanged(s, e) {

        cbAllCant.SetChecked(s.GetSelectedRowCount() == s.cpVisibleRowCount);

        if (e.isChangedOnServer == false) {
            if (e.isAllRecordsOnPage && e.isSelected) {
                _selectNumberOverPageCant = s.GetVisibleRowsOnPage();
                _selectNumberCant = _selectNumberCant + s.GetVisibleRowsOnPage(); // when all rows are selected within the page
            }
            else if (e.isAllRecordsOnPage && !e.isSelected) {
                _selectNumberCant = _selectNumberCant - s.GetVisibleRowsOnPage(); // when all rows are deselected within the page
                if (_selectNumberCant == 0)
                    _selectNumberOverPageCant = 0;
            }
            else if (!e.isAllRecordsOnPage && e.isSelected) {
                selectionTypeCant = "sRow#Cant";

                _selectNumberCant++; // when one row is selected
                _selectNumberOverPageCant++;
            }
            else if (!e.isAllRecordsOnPage && !e.isSelected) {
                selectionTypeCant = "uRow#Cant";

                _selectNumberCant--; // when one row is deselected
                _selectNumberOverPageCant--;
            }

            if (_handleCant) { // if the selection wasn’t performed by clicking the cbPageCant
                cbPageCant.SetChecked(_selectNumberOverPageCant == s.GetVisibleRowsOnPage()); // let’s change the cbPageCant state if needed
                _handleCant = false;
            }

            _handleCant = true;
        }
        else {
            cbPageCant.SetChecked(cbAllCant.GetChecked()); // if the selection was performed on the server, let’s check cbPageCant
        }

        gridMasterSelectionChange.PerformCallback(selectionTypeCant + '#' + s.GetSelectedRowCount());
    }

    function OnGridCantEndCallback(s, e) {

        if (grid2.cpPageChanged == 1) {
            _selectNumberOverPageCant = 0;
        }

        if (selectionTypeCant == 'sAll#Cant')
            _selectNumberCant = s.cpVisibleRowCount;
        else if (selectionTypeCant == 'uAll#Cant') {
            _selectNumberCant = 0;
        }
    }

    function OnAllCantCheckedChanged(s, e) {
        if (s.GetChecked()) {
            DisplayJConfirm('Power', s.cpMessage, function (r) {
                if (r) {
                    grid2.SelectRows();
                    selectionTypeCant = "sAll#Cant";
                }
                else {
                    s.SetChecked(false);
                    selectionTypeCant = "uAll#Cant";
                    grid2.UnselectRows();
                }
            });
        }
        else {
            s.SetChecked(false);
            selectionTypeCant = "uAll#Cant";
            grid2.UnselectRows();
        }

        // gestione dell'abilitazione/disabilitazione del tasto di lancio export
        EnableOrDisableBtnExport();
    }

    function OnPageCantCheckedChanged(s, e) {
        _handleCant = false;

        if (s.GetChecked()) {
            selectionTypeCant = "sPage#Cant";
            grid2.SelectAllRowsOnPage();
        }

        else {
            selectionTypeCant = "uPage#Cant";
            grid2.UnselectAllRowsOnPage();
        }

        // gestione dell'abilitazione/disabilitazione del tasto di lancio export
        EnableOrDisableBtnExport();
    }

    // ----------------------------------- gestione nascondimento/visualizzazione e scambio griglie ---------------------------------------
    // nel caricamento della pagina mi occupo della visualizzazione o meno delle griglie; della disabilitazione o meno del pulsante di lancio export;
    // e di nascondere alcuni pulsanti standard
    $(window).on('load', function () { hideBlocks(); EnableOrDisableBtnExport(); disableStandardButtons(); });
    function cbpExportTypeChanged_OnEndCallback(s, e) {
        // recupero dal server side (valore calcolato nel callback)
        // il nome dell'entità principale di selezione
        var entityType = s.cpSelectedEntity;

        if (entityType == 'Col') { // nella griglia master sono visualizzati i collaboratori e nella griglia slave i cantieri
            // visualizzazione dei blocchi eventualmente nascosti
            showBlocks();

            // imposto la dimensione di lunghezza della griglia del collaboratore/cantiere di modo da
            // stare correttamente a livello della tabella di esportazione in alto (così da non sforare lo schermo)
            $('#colGridDiv').css('width', $('#exportParamTable').css('width'));
            $('#cantGridDiv').css('width', $('#exportParamTable').css('width'));
            $('#masterGridDiv').prepend($('#colGridDiv'));
            $('#slavePanelDiv').prepend($('#cantGridDiv'));
        }
        else if (entityType == 'Cant') { // nella griglia master sono visualizzati i cantieri e nella griglia slave i collaboratori
            // visualizzazione dei blocchi eventualmente nascosti
            showBlocks();

            // imposto la dimensione di lunghezza della griglia del collaboratore/cantiere di modo da
            // stare correttamente a livello della tabella di esportazione in alto (così da non sforare lo schermo)
            $('#colGridDiv').css('width', $('#exportParamTable').css('width'));
            $('#cantGridDiv').css('width', $('#exportParamTable').css('width'));
            $('#masterGridDiv').prepend($('#cantGridDiv'));
            $('#slavePanelDiv').prepend($('#colGridDiv'));
        }
        else {
            // non c'è un'entità riconosciuta, nascondo il pulsante di ulteriore selezione e le griglie
            hideBlocks();
        }

        // se è richiesta la visualizzazione dei moduli di tipo calcolo e tipologia ore allora li si visualizza,
        // altrimenti si procede al loro nascondimento
        var showDetailBlock = s.cpShowDetail;
        var showCalculationBlock = s.cpShowCalculationType;
        var showHourBlock = s.cpShowHoursType;
        var showDurationTolleranceBlock = s.cpShowDurationTollerance;
        var showEUTolleranceBlock = s.cpShowEUTollerance;
        if (showCalculationBlock)
            showAdditionalElement('#calculationTypeTD');
        else
            hideAdditionalElement('#calculationTypeTD');

        if (showHourBlock)
            showAdditionalElement('#hourTypeTD');
        else
            hideAdditionalElement('#hourTypeTD');

        if (showDurationTolleranceBlock)
            showAdditionalElement('#tolleranceDurationTD');
        else
            hideAdditionalElement('#tolleranceDurationTD');

        if (showEUTolleranceBlock)
            showAdditionalElement('#tolleranceEUTD');
        else
            hideAdditionalElement('#tolleranceEUTD');

        if (showDetailBlock)
            showAdditionalElement('#dettaglioTD');
        else
            hideAdditionalElement('#dettaglioTD');


        // se entrambe le opzioni di riga sono nascoste allora ne recupero lo spazio nascondendo la riga,
        // altrimenti forzo la sua visualizzazione
        if (!showEUTolleranceBlock && !showDurationTolleranceBlock)
            $('#tolleranceTR').hide();
        else
            $('#tolleranceTR').show();

        if (!showCalculationBlock && !showHourBlock)
            $('#calculationTR').hide();
        else
            $('#calculationTR').show();

        // ogni volta che si cambia di tipo export in ogni caso va azzerata la selezione di collaboratori/cantieri effettuata
        grid.UnselectRows();
        grid2.UnselectRows();

        // calcolo lo stato del pulsante di lancio elaborazione
        EnableOrDisableBtnExport();

    }

    // nasconde un elemento di selezione aggiuntiva alla maschera di export utilizzando
    // lo secifico selettore jquery
    function hideAdditionalElement(additionalElementSelector) {
        $(additionalElementSelector).css('visibility', 'hidden');
    }

    // visualizza un elemento di selezione aggiuntiva alla maschera di export utilizzando
    // lo specifico selettore jquery
    function showAdditionalElement(additionalElementSelector) {
        $(additionalElementSelector).css('visibility', 'visible');
    }

    // visualizza i blocchi delle griglie/pulsanti
    function showBlocks() {
        $('#otherSelectionButtonDiv').css('visibility', 'visible');
        $('#colGridDiv').css('visibility', 'visible');
        $('#cantGridDiv').css('visibility', 'visible');
        $('#masterGridDiv').css('visibility', 'visible');
        $('#slavePanelDiv').css('visibility', 'visible');
    }

    // nasconde i blocchi delle griglie/pulsanti
    function hideBlocks() {
        $('#otherSelectionButtonDiv').css('visibility', 'hidden');
        $('#colGridDiv').css('visibility', 'hidden');
        $('#cantGridDiv').css('visibility', 'hidden');
        $('#masterGridDiv').css('visibility', 'hidden');
        $('#slavePanelDiv').css('visibility', 'hidden');
    }

    // ----------------------------------- gestione abilitazione/disabilitazione pulsante lancia export ---------------------------------------

    // verifica che le condizioni di lancio dell'export e in base alla correttezza delle stesse
    // abilita o disabilita il pulsnte
    function EnableOrDisableBtnExport() {

        // 1. Per essere abilitato il pulsante deve essere popolato il tipo export
        var exportCondition = $('#Tipo_Export').val() == 'undefined' || $('#Tipo_Export').val() == '' ? false : true;

        // 2. Per essere abilitato il pulsante deve essere valorizzato il periodo
        var periodCondition = dePeriodo.GetText() == 'undefined' || dePeriodo.GetText() == '' ? false : true;

        // 3. Per essere abilitato il pulsante deve essere selezionato almeno un valore sulla griglia master
        var selectionCondition = gridMasterSelectionChange.cpMasterSelectedCount > 0 ? true : false;

        // si abilita il pulsante solamente se tutte le condizioni sono verificate, altrimenti si disabilita
        btnlLaunchExport.SetEnabled(exportCondition && periodCondition && selectionCondition);
    }

    // ----------------------------------- disabilitazione pulsanti standard ---------------------------------------

    function disableStandardButtons() {
        btnPrintXlsx.SetVisible(false);
        btnPrintPdf.SetVisible(false);
    }

    // -------------------------------------- GESTIONE DELLA SELEZIONE SOLO MESE/ANNO IN CAMPO PERIODO -----------------------------------------
    function OndePeriodo_Init(s, e) {
        var calendar = s.GetCalendar();
        calendar.owner = s;
        calendar.GetMainElement().style.opacity = '0';
    }

    function OndePeriodo_DropDown(s, e) {
        var calendar = s.GetCalendar();
        var fastNav = calendar.fastNavigation;
        fastNav.activeView = calendar.GetView(0, 0);
        fastNav.Prepare();
        fastNav.GetPopup().popupVerticalAlign = "Below";
        fastNav.GetPopup().ShowAtElement(s.GetMainElement())

        fastNav.OnOkClick = function () {
            var parentDateEdit = this.calendar.owner;
            var currentDate = new Date(fastNav.activeYear, fastNav.activeMonth, 1);
            parentDateEdit.SetDate(currentDate);
            parentDateEdit.HideDropDown();
            EnableOrDisableBtnExport();
        }

        fastNav.OnCancelClick = function () {
            var parentDateEdit = this.calendar.owner;
            parentDateEdit.HideDropDown();
            EnableOrDisableBtnExport();
        }
    }
</script>

<style>
    #dettaglioTD label
{  
    margin-left: 5px; 
}

</style>

<dx:ASPxFormLayout ID="flStandardInsert" ClientInstanceName="flStandardInsert" runat="server" Width="100%">
    <ClientSideEvents Init="function (s, e) { cbpExportTypeChanged_OnEndCallback(cbpExportTypeChanged, null); }"></ClientSideEvents>
    <Items>
        <dx:LayoutGroup Caption="" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>

                            <!-- CALLBACK PANEL UTILIZZATO PER LA GESTIONE DEL CAMBIO SELEZIONE DEL TIPO EXPORT -->
                            <dx:ASPxCallbackPanel runat="server" ID="CbpExportTypeChanged" ClientInstanceName="cbpExportTypeChanged" OnCallback="CbpExportTypeChanged_OnCallback" ClientSideEvents-EndCallback="cbpExportTypeChanged_OnEndCallback">
                                <ClientSideEvents EndCallback="cbpExportTypeChanged_OnEndCallback"></ClientSideEvents>
                                <PanelCollection>
                                    <dx:PanelContent runat="server">

                                        <!-- TABELLA CON SELEZIONE PERIODO E TIPO EXPORT -->
                                        <table id="exportParamTable" style="width: 100%;">
                                            <tr>
                                                <td style="width: 33%; text-align: center;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel ID="lblTipoExport" Text="Tipo export:" runat="server" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxComboBox ID="cmbTipoExport" ClientInstanceName="cmbTipoExport" runat="server" Width="380px" ClientEnabled="True">
                                                                    <ClientSideEvents SelectedIndexChanged="function (s, e) { cbpExportTypeChanged.PerformCallback(); }"></ClientSideEvents>
                                                                </dx:ASPxComboBox>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td style="width: 33%; text-align: center;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel ID="lblPeriodo" runat="server" Text="Periodo:"></dx:ASPxLabel>
                                                            </td>
                                                            <td>
                                                                <dx:ASPxDateEdit ID="dePeriodo" runat="server" ClientInstanceName="dePeriodo" EditFormatString="MMM yyyy" DisplayFormatString="MMM yyyy" ShowShadow="False">
                                                                    <ClientSideEvents ValueChanged="function (s, e){ EnableOrDisableBtnExport(); }" DropDown="OndePeriodo_DropDown" Init="OndePeriodo_Init" />
                                                                </dx:ASPxDateEdit>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td style="width: 33%; text-align: center;">
                                                    <!-- ABILITARE PULSANTE SOLAMENTE SE PERIODO ED EXPORT SELEZIONATO + METTERE IN LINGUA -->
                                                    <dx:ASPxButton ID="BtnLaunchExport" ClientInstanceName="btnlLaunchExport" Text="Lancia Export" runat="server" OnClick="BtnLaunchExport_OnClick" AutoPostBack="False" />
                                                </td>
                                            </tr>
                                               <tr>
                                                <td id="dettaglioTD" style="width: 33%; text-align: center;margin-bottom:auto; visibility: hidden;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxCheckBox runat="server" ID="ASPxCheckBoxDetali" ClientInstanceName="cbExportDetail" Text="prova"/>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr id="calculationTR">
                                                <td id="calculationTypeTD" style="width: 33%; text-align: center; visibility: hidden;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel runat="server" ID="LblTipoCalcolo" ClientInstanceName="lblTipoCalcolo" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxRadioButtonList runat="server" ID="RdBtnTipoCalcolo" ClientInstanceName="rdBtnTipoCalcolo" RepeatDirection="Horizontal"
                                                                    RepeatColumns="2" Border-BorderWidth="0">
                                                                    <Border BorderWidth="0px"></Border>
                                                                </dx:ASPxRadioButtonList>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td id="hourTypeTD" style="width: 33%; text-align: center; visibility: hidden;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel runat="server" ID="LblTipoOre" ClientInstanceName="lblTipoOre" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxRadioButtonList runat="server" ID="RdBtnTipoOre" ClientInstanceName="rdBtnTipoOre" RepeatDirection="Horizontal"
                                                                    RepeatColumns="3" Border-BorderWidth="0">
                                                                    <Border BorderWidth="0px"></Border>
                                                                </dx:ASPxRadioButtonList>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td style="width: 33%; text-align: center;"></td>
                                            </tr>
                                            <tr id="tolleranceTR">
                                                <td id="tolleranceDurationTD" style="width: 33%; text-align: center; visibility: hidden;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel runat="server" ID="LblTolleranzaDurata" ClientInstanceName="lblTolleranzaDurata" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxSpinEdit runat="server" ID="SpedtTolleranzaDurata" ClientInstanceName="spedtTolleranzaDurata" DecimalPlaces="0" MinValue="0" MaxValue="1440" Number="0" />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td id="tolleranceEUTD" style="width: 33%; text-align: center; visibility: hidden;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel runat="server" ID="LblTolleranzaEU" ClientInstanceName="lblTolleranzaEU" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxSpinEdit runat="server" ID="SpedtTolleranzaEU" ClientInstanceName="spedtTolleranzaEU" DecimalPlaces="0" MinValue="0" MaxValue="1440" Number="0" />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td style="width: 33%; text-align: center; visibility: hidden;"></td>
                                            </tr>
                                         
                                        </table>

                                        <!-- PANNELLO DI VISUALIZZAZIONE DELLA PRIMA GRIGLIA (PRIMARIA) -->
                                        <dx:ASPxPanel ID="MasterGridPanel" ClientInstanceName="MasterGridPanel" runat="server">
                                            <PanelCollection>
                                                <dx:PanelContent runat="server">
                                                    <div id="masterGridDiv" style="visibility: hidden;">
                                                    </div>
                                                </dx:PanelContent>
                                            </PanelCollection>
                                        </dx:ASPxPanel>

                                        <!-- PULSANTE PER LANCIO ULTERIORI SELEZIONI -->
                                        <div id="otherSelectionButtonDiv" style="visibility: hidden; padding-top: 5px; padding-bottom: 5px;">
                                            <dx:ASPxButton ID="BtnUltSel" runat="server" Text="Attiva ulteriori selezioni" AutoPostBack="False" ClientSideEvents-Click="function (s, e) { pcSlaveGrid.Show(); }">
                                                <ClientSideEvents Click="function (s, e) { pcSlaveGrid.Show(); }"></ClientSideEvents>
                                            </dx:ASPxButton>
                                            <br />
                                            <dx:ASPxCallbackPanel ID="CbpOtherSelectionStatus" ClientInstanceName="cbpOtherSelectionStatus" OnCallback="CbpOtherSelectionStatus_OnCallback" runat="server">
                                                <PanelCollection>
                                                    <dx:PanelContent runat="server">
                                                        <dx:ASPxLabel ID="LblOtherSelectionState" ClientInstanceName="lblOtherSelectionState" Text="" runat="server" />
                                                    </dx:PanelContent>
                                                </PanelCollection>
                                            </dx:ASPxCallbackPanel>
                                        </div>

                                        <!-- POPUP DI VISUALIZZAZIONE DELLA SECONDA GRIGLIA (AGGIUNTIVA) -->
                                        <dx:ASPxPopupControl ID="pcSlaveGrid" ClientInstanceName="pcSlaveGrid" runat="server" CloseAction="CloseButton" Modal="True"
                                            PopupHorizontalAlign="WindowCenter" PopupVerticalAlign="WindowCenter" HeaderText="" AllowDragging="True" PopupAnimationType="None"
                                            EnableViewState="False">
                                            <ClientSideEvents PopUp="function(s, e) { ASPxClientEdit.ClearGroup('entryGroup'); }" CloseUp="function (s, e){ cbpOtherSelectionStatus.PerformCallback(); }" />
                                            <ContentCollection>
                                                <dx:PopupControlContentControl runat="server">
                                                    <dx:ASPxPanel ID="SlaveGridPanel" ClientInstanceName="SlaveGridPanel" Width="100%" runat="server">
                                                        <PanelCollection>
                                                            <dx:PanelContent runat="server">
                                                                <div id="slavePanelDiv" style="visibility: hidden;">
                                                                </div>
                                                            </dx:PanelContent>
                                                        </PanelCollection>
                                                    </dx:ASPxPanel>
                                                </dx:PopupControlContentControl>
                                            </ContentCollection>
                                        </dx:ASPxPopupControl>

                                        <!-- CALLBACK UTILIZZATO PER IL CAMBIO SELEZIONE NELLA GRIGLIA MASTER -->
                                        <dx:ASPxCallback runat="server" ID="gridMasterSelectionChange" ClientInstanceName="gridMasterSelectionChange" OnCallback="gridMasterSelectionChange_OnCallback">
                                            <ClientSideEvents EndCallback="function (s, e) { EnableOrDisableBtnExport(); }"></ClientSideEvents>
                                        </dx:ASPxCallback>

                                        <!-- GRIGLIA DI VISUALIZZAZIONE E SELEZIONE DEI COLLABORATORI -->
                                        <div id="colGridDiv" style="visibility: hidden;">
                                            <dx:ASPxGridView ID="gvColExport" ClientInstanceName="gvColExport" runat="server" AutoGenerateColumns="False" Width="100%" OnPageIndexChanged="gvColExport_OnPageIndexChanged" OnCustomJSProperties="gvColExport_OnCustomJSProperties" OnInit="gvColExport_OnInit">
                                                <ClientSideEvents SelectionChanged="OnGridColSelectionChanged" EndCallback="OnGridColEndCallback" />
                                                <Columns>
                                                    <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0">
                                                        <HeaderTemplate>
                                                            <dx:ASPxCheckBox ID="cbAllCol" runat="server" ClientInstanceName="cbAllCol" ToolTip="Select all rows" BackColor="White" OnInit="cbAllCol_Init" OnCustomJSProperties="cbAllCol_OnCustomJSProperties">
                                                                <ClientSideEvents CheckedChanged="OnAllColCheckedChanged" />
                                                            </dx:ASPxCheckBox>
                                                            <dx:ASPxCheckBox ID="cbPageCol" runat="server" ClientInstanceName="cbPageCol" ToolTip="Select all rows within the page" OnInit="cbPageCol_Init">
                                                                <ClientSideEvents CheckedChanged="OnPageColCheckedChanged" />
                                                            </dx:ASPxCheckBox>
                                                        </HeaderTemplate>
                                                        <HeaderStyle HorizontalAlign="Center" />
                                                    </dx:GridViewCommandColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Codice_Collaboratore" VisibleIndex="10" Width="10%">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Codice_Domicilio_Luogo_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Codice_Fiscale_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Nascita_Luogo_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Residenza_Luogo_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Cognome_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="CognomeNome_Col" VisibleIndex="20" Width="25%">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Col" Visible="False">
                                                    </dx:GridViewDataDateColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Col" VisibleIndex="200" Width="5%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Domicilio_Localita_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Nascita_Cap_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataDateColumn FieldName="Nascita_Data_Col" Visible="False">
                                                    </dx:GridViewDataDateColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Nascita_Luogo_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Nascita_Provincia_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Nazionalita_Col" VisibleIndex="70" Width="7%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Nome_Col" Visible="false">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Patente_Col" VisibleIndex="110" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Qualifica_Col" VisibleIndex="80" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Col" Visible="True">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Residenza_Cap_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Indirizzo_Col" VisibleIndex="50" Width="15%">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Col" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Residenza_Luogo_Col" VisibleIndex="40" Width="15%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_Col" VisibleIndex="55" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Sesso_Col" VisibleIndex="100" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Straniero_CEE_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Straniero_Col" VisibleIndex="60" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Stato_Civile_Col" VisibleIndex="90" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Col" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                </Columns>
                                            </dx:ASPxGridView>
                                        </div>

                                        <!-- GRIGLIA DI GESTIONE DELLA SELEZIONE DEI CANTIERI -->
                                        <div id="cantGridDiv" style="visibility: visible;">
                                            <dx:ASPxGridView ID="gvCantExport" ClientInstanceName="gvCantExport" runat="server" AutoGenerateColumns="False" Width="100%" OnInit="gvCantExport_OnInit">
                                                <ClientSideEvents SelectionChanged="OnGridCantSelectionChanged" EndCallback="OnGridCantEndCallback" />
                                                <Columns>
                                                    <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0">
                                                        <HeaderTemplate>
                                                            <dx:ASPxCheckBox ID="cbAllCant" runat="server" ClientInstanceName="cbAllCant" ToolTip="Select all rows" BackColor="White" OnInit="cbAllCant_Init" OnCustomJSProperties="cbAllCant_OnCustomJSProperties">
                                                                <ClientSideEvents CheckedChanged="OnAllCantCheckedChanged" />
                                                            </dx:ASPxCheckBox>
                                                            <dx:ASPxCheckBox ID="cbPageCant" runat="server" ClientInstanceName="cbPageCant" ToolTip="Select all rows within the page" OnInit="cbPageCant_Init">
                                                                <ClientSideEvents CheckedChanged="OnPageCantCheckedChanged" />
                                                            </dx:ASPxCheckBox>
                                                        </HeaderTemplate>
                                                        <HeaderStyle HorizontalAlign="Center" />
                                                    </dx:GridViewCommandColumn>
                                                    <dx:GridViewDataDateColumn FieldName="LastDateActiveFru" ReadOnly="True" VisibleIndex="1" />
                                                    <dx:GridViewDataTextColumn FieldName="LastFruCode" ReadOnly="True" VisibleIndex="2"></dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="LastFruNSerie" ReadOnly="True" VisibleIndex="2"></dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia1_Can" Visible="False">
                                                        <PropertiesSpinEdit DecimalPlaces="2" />
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia2_Can" Visible="False">
                                                        <PropertiesSpinEdit DecimalPlaces="2" />
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="True">
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
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Can" VisibleIndex="40" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Domicilio_Interno_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Domicilio_Localita_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Can" VisibleIndex="50" Width="15%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Domicilio_Luogo_Can" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Can" VisibleIndex="30" Width="5%">
                                                    </dx:GridViewDataComboBoxColumn>
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
                                                    <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Notte_Ril_Can" Visible="False">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Ril_Can" Visible="False">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Durata_Max_Ril_Can" Visible="False">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Durata_Min_Ril_Can" Visible="False">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
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
                                                    <dx:GridViewDataTextColumn FieldName="LatitudineGps_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Limite_Inizio_Notte_Can" Visible="False">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
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
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tolleranza_F_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
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
                                                        <CellStyle HorizontalAlign="Center"></CellStyle>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Nr_Isee_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Bimbi_Fascia_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_GG_Lavorativi" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia1_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia2_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PreScuola_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_1Mezzora_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_2Mezzora_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Ore_Massime_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
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
                                                        <CellStyle HorizontalAlign="Center"></CellStyle>
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Provincia_Nascita_Can" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="RaggioGps_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Can" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento2_Can" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Can" Visible="False">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Sesso_Can" Visible="False">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="Singola_Reg" VisibleIndex="90" Width="7%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_F_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Soglia_Durata_Can" Visible="False">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
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
                                            </dx:ASPxGridView>
                                        </div>
                                    </dx:PanelContent>
                                </PanelCollection>
                            </dx:ASPxCallbackPanel>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>

            <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>
