<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TimesheetModule.ascx.cs" Inherits="PowerWeb.Modules.TimesheetModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallback" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxFormLayout" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<script type="text/javascript">

    function getCurrentTimesheetGrid() {
        if (chkShowWeeklyTotals.GetChecked())
            return gvTimesheetWeeklyTotals;
        else
            return gvTimesheet;

    }

    function btnLoad_OnClick(s, e) {
        var date = deTimesheet.GetDate();
        if (date) {
            // calcolo delle opzioni selezionate nel listbox
            var listboxOptions = $('#timesheetHeaderDiv').attr('selectedOptions');
            getCurrentTimesheetGrid().PerformCallback(date.getDate() + '/' + (date.getMonth() + 1) + '/' + date.getFullYear() + '#' + Tipo_Entita_Timesheet.GetValue() + '@' + listboxOptions);
        }
    }

    function deTimesheet_OnValueChanged(s, e) {
        var date = deTimesheet.GetDate();
        if (date)
            btnLoad.SetEnabled(true);
        else
            btnLoad.SetEnabled(false);
    }

    function btnTSMCustomizeColumns_OnClick(s, e) {

        if (getCurrentTimesheetGrid().IsCustomizationWindowVisible())
            getCurrentTimesheetGrid().HideCustomizationWindow();
        else
            getCurrentTimesheetGrid().ShowCustomizationWindow();
    }

    function cmbTSMLayout_OnSelectedIndexChanged(s, e) {
        var t = cmbTSMLayout.GetSelectedItem();
        if (t == null) {
            cpTSMLayout.PerformCallback('save');
        } else {
            cpTSMLayout.PerformCallback(t.value);
        }
    }

    function cbInsertCorrection_EndCallback(s, e) {
        if (s.cpErrorMessage != null && s.cpErrorMessage != '') {
            DisplayDialogError('Power', s.cpErrorMessage);
        } else {
            // se l'operazione è andata a buon fine si procede al ricalcolo della griglia del cartellino
            btnLoad_OnClick(null, null);
        }
    }

    function GetListboxSelectedValues(listBox) {
        // recupero gli elementi selezionati dall'utente
        var selectedItems = listBox.GetSelectedItems();

        // calcolo i codici (valore salvato) di ogni elemento selezionato e lo imposto in una stringa separata da virgola
        var texts = [];
        for (var i = 0; i < selectedItems.length; i++) {
            if (typeof selectedItems[i] != 'undefined') {
                if (typeof selectedItems[i].value != 'undefined') {
                    texts.push(selectedItems[i].value);
                }
            }
        }


        // effettuo il join degli elementi trovati in una stringa separata da ';'
        // e ritorno quanto calcolato
        return texts.join(';');
    }

    function OnListBoxSelectionChanged(listBox, args) {
        var selectedItemValue = GetListboxSelectedValues(listBox);
        $('#timesheetHeaderDiv').attr('selectedOptions', selectedItemValue);
    }


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
    }

    // ----------------------------------- gestione nascondimento/visualizzazione e scambio griglie ---------------------------------------
    // nel caricamento della pagina mi occupo della visualizzazione o meno delle griglie;
    $(window).on('load', function () { hideBlocks(); });
    function cbpEntityTypeSelector_OnEndCallback(s, e) {
        // recupero dal server side (valore calcolato nel callback)
        // il nome dell'entità principale di selezione
        var entityType = s.cpSelectedEntity;

        // se ho appena salvato la vista (proprietà undefined)
        // allora recupero l'entity type dal combobox
        if (typeof entityType == 'undefined')
            entityType = Tipo_Entita_Timesheet.GetValue();

        if (entityType == 'Col') { // nella griglia master sono visualizzati i collaboratori e sono invece nascosti i cantieri
            $('#colGridDiv').css('display', 'block');
            $('#cantGridDiv').css('display', 'none');

            // imposto la dimensione di lunghezza della griglia del collaboratore/cantiere di modo da
            // stare correttamente a livello della tabella di esportazione in alto (così da non sforare lo schermo)
            $('#colGridDiv').css('width', $('#selectionEntityTable').css('width'));
            $('#cantGridDiv').css('width', $('#selectionEntityTable').css('width'));
        }
        else if (entityType == 'Can') { // nella griglia master sono visualizzati i collaboratori e sono invece nascosti i cantieri
            $('#colGridDiv').css('display', 'none');
            $('#cantGridDiv').css('display', 'block');

            // imposto la dimensione di lunghezza della griglia del collaboratore/cantiere di modo da
            // stare correttamente a livello della tabella di esportazione in alto (così da non sforare lo schermo)
            $('#colGridDiv').css('width', $('#selectionEntityTable').css('width'));
            $('#cantGridDiv').css('width', $('#selectionEntityTable').css('width'));
        }
        else {
            // non c'è un'entità riconosciuta, nascondo il pulsante di ulteriore selezione e le griglie
            hideBlocks();
        }

        // ogni volta che si cambia di tipo entità in ogni caso va azzerata la selezione di collaboratori/cantieri effettuata
        grid.UnselectRows();
        grid2.UnselectRows();

    }

    // nasconde i blocchi delle griglie/pulsanti visualizzando quanto previsto di default
    function hideBlocks() {
        $('#colGridDiv').css('display', 'block');
        $('#cantGridDiv').css('display', 'none');
    }

    // rende visibili i blocchi delle griglie/pulsanti
    function showBlocks() {
        $('#colGridDiv').css('display', 'block');
        $('#cantGridDiv').css('display', 'block');
    }

    // -------------------------------------- GESTIONE DELLA SELEZIONE SOLO MESE/ANNO IN CAMPO PERIODO -----------------------------------------
    function OndeTimesheet_Init(s, e) {
        var calendar = s.GetCalendar();
        calendar.owner = s;
        calendar.GetMainElement().style.opacity = '0';
    }

    function OndeTimesheet_DropDown(s, e) {
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
        }

        fastNav.OnCancelClick = function () {
            var parentDateEdit = this.calendar.owner;
            parentDateEdit.HideDropDown();
        }
    }

    // -------------------------------------- GESTIONE DEI TOTALI MENSILI/SETTIMANALI DEL CARTELLINO -----------------------------------------
    function TimesheetTotalsChanged() {
        pnlTimesheetGrids.PerformCallback();
    }

    // ----------------------------------- GESTIONE DEL PULSANTE DI SVUOTAMENTO COMBOBOX -----------------------------------------------------
    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }

</script>

<%-- LAYOUT FORM UTILIZZATO PER CONTENERE LE GRIGLIE DI SELEZIONE DEI DATI --%>
<dx:ASPxFormLayout ID="flMasterSelector" ClientInstanceName="flMasterSelector" runat="server" Width="100%">
    <ClientSideEvents Init="function (s, e) {cbpEntityTypeSelector_OnEndCallback(cbpEntityTypeSelector, null); }"></ClientSideEvents>
    <Items>
        <dx:LayoutGroup Caption="Selezione dati da visualizzare" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>

                            <%--CALLBACK PANEL UTILIZZATO PER LA GESTIONE DEL CAMBIO SELEZIONE DEL TIPO ENTITA'--%>
                            <dx:ASPxCallbackPanel runat="server" ID="CbpEntityTypeSelector" ClientInstanceName="cbpEntityTypeSelector" OnCallback="CbpEntityTypeSelector_OnCallback" ClientSideEvents-EndCallback="cbpEntityTypeSelector_OnEndCallback">
                                <PanelCollection>
                                    <dx:PanelContent runat="server">

                                        <%--TABELLA CON SELEZIONE TIPO DI ENTITA' DA UTILIZZARE--%>
                                        <table id="selectionEntityTable" style="width: 100%;">
                                            <tr>
                                                <td style="width: 33%; text-align: center;">
                                                    <table>
                                                        <tr>
                                                            <td>
                                                                <dx:ASPxLabel ID="lblEntityType" Text="Anagrafica da visualizzare:" runat="server" />
                                                            </td>
                                                            <td>
                                                                <dx:ASPxComboBox ID="cmbEntityType" ClientInstanceName="cmbEntityType" runat="server" Width="380px" ClientEnabled="True">
                                                                    <ClientSideEvents SelectedIndexChanged="function (s, e) { cbpEntityTypeSelector.PerformCallback(); }"></ClientSideEvents>
                                                                </dx:ASPxComboBox>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>

                                        <%--PANNELLO DI VISUALIZZAZIONE DELLA GRIGLIA MASTER--%>
                                        <dx:ASPxPanel ID="MasterGridPanel" ClientInstanceName="MasterGridPanel" runat="server">
                                            <PanelCollection>
                                                <dx:PanelContent runat="server">
                                                    <div id="masterGridDiv">
                                                        <%--CALLBACK UTILIZZATO PER IL CAMBIO SELEZIONE NELLA GRIGLIA MASTER--%>
                                                        <dx:ASPxCallback runat="server" ID="gridMasterSelectionChange" ClientInstanceName="gridMasterSelectionChange" OnCallback="gridMasterSelectionChange_OnCallback">
                                                        </dx:ASPxCallback>

                                                        <%-- GRIGLIA DI SELEZIONE DEI COLLABORATORI --%>
                                                        <dx:ASPxCallback runat="server" ID="gridSelectionChange" ClientInstanceName="gridSelectionChange" OnCallback="gridSelectionChange_OnCallback"></dx:ASPxCallback>
                                                        <div id="colGridDiv" style="display: none;">
                                                            <dx:ASPxGridView ID="gvColSel" runat="server" AutoGenerateColumns="False" Width="100%" OnPageIndexChanged="gvColSel_OnPageIndexChanged" OnCustomJSProperties="gvColSel_OnCustomJSProperties" OnCustomCallback="gvColSel_OnCustomCallback">
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
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="false">
                                                                        <Settings AllowHeaderFilter="False" />
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Codice_Collaboratore" VisibleIndex="10" Width="10%">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Domicilio_Luogo_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Nascita_Luogo_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Codice_Residenza_Luogo_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="1">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Cognome_Col" Visible="false">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="CognomeNome_Col" VisibleIndex="20" Width="25%">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Fine_Col" Visible="False">
                                                                    </dx:GridViewDataDateColumn>
                                                                    <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Inizio_Col" Visible="False">
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
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataDateColumn FieldName="LastDateActivePru" ReadOnly="True" VisibleIndex="2" />
                                                                    <dx:GridViewDataDateColumn FieldName="LastReg" ReadOnly="True" VisibleIndex="5" />
                                                                    <dx:GridViewDataTextColumn FieldName="LastPruCode" ReadOnly="True" VisibleIndex="3"></dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="LastPruNSerie" ReadOnly="True" VisibleIndex="4"></dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Livello_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Matricola_Col" VisibleIndex="80" Width="7%">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="N_Pru_Col" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Nazionalita_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Nome_Col" Visible="false">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataSpinEditColumn FieldName="Prova" Visible="False">
                                                                    </dx:GridViewDataSpinEditColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Qualifica_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
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
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Sesso_Col" VisibleIndex="60" Width="5%">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Id" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Rapporto_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Contratto_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Titolo_Studio_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Zona_Col" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                </Columns>
                                                            </dx:ASPxGridView>
                                                        </div>
                                                        <div id="cantGridDiv" style="display: none;">
                                                            <dx:ASPxGridView ID="gvCantSel" ClientInstanceName="gvCantSel" runat="server" AutoGenerateColumns="False" Width="100%" OnCustomJSProperties="gvCantSel_OnCustomJSProperties" OnCustomCallback="gvCantSel_OnCustomCallback">
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
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="True">
                                                                        <Settings AllowHeaderFilter="False" />
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Cap_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Cap_Nascita_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Cli_Id" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
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
                                                                    <dx:GridViewDataTextColumn FieldName="Descrizione_Can" VisibleIndex="30" Width="20%">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Can" VisibleIndex="110" Width="5%">
                                                                    </dx:GridViewDataCheckColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Fil_Id" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Indirizzo_Can" VisibleIndex="70" Width="25%">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="LatitudineGps_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Livello_Assistito_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Luogo_Can" VisibleIndex="60" Width="20%">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Luogo_Nascita_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Nazione_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Nazione_Nascita_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Nome_Assistito_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Provincia_Can" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataSpinEditColumn FieldName="RaggioGps_Can" Visible="False">
                                                                    </dx:GridViewDataSpinEditColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento2_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipo_Cantiere_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataTextColumn FieldName="Tipo_Interv_Can" Visible="False">
                                                                    </dx:GridViewDataTextColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tipologia_Can" VisibleIndex="10" Width="7%">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Zona_Can" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Id_Cant" Visible="False">
                                                                    </dx:GridViewDataComboBoxColumn>
                                                                    <dx:GridViewDataDateColumn FieldName="LastReg" Visible="False">
                                                                    </dx:GridViewDataDateColumn>
                                                                </Columns>
                                                            </dx:ASPxGridView>
                                                        </div>
                                                    </div>
                                                </dx:PanelContent>
                                            </PanelCollection>
                                        </dx:ASPxPanel>

                                    </dx:PanelContent>
                                </PanelCollection>
                            </dx:ASPxCallbackPanel>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>


<dx:ASPxCallback runat="server" ID="cbInsertCorrection" ClientInstanceName="cbInsertCorrection" OnCallback="CbInsertCorrection_OnCallback"
    ClientSideEvents-EndCallback="cbInsertCorrection_EndCallback" />

<dx:ASPxCallbackPanel ID="cpTSMLayout" runat="server" Width="100%" ClientInstanceName="cpTSMLayout" OnCallback="cpTSMLayout_Callback">
    <PanelCollection>
        <dx:PanelContent ID="PanelContent2" runat="server" SupportsDisabledAttribute="True">
            <dx:ASPxFormLayout ID="flRegCorrection" runat="server" Width="100%">
                <Items>
                    <dx:LayoutGroup Caption="Rettifiche a registrazioni" SettingsItemHelpTexts-Position="Bottom">
                        <Items>
                            <dx:LayoutItem Caption=" " HelpText="">
                                <LayoutItemNestedControlCollection>
                                    <dx:LayoutItemNestedControlContainer>
                                        <table style="width: 100%">
                                            <tr>
                                                <td style="margin-left: 5px;">
                                                    <dx:ASPxLabel runat="server" ID="lblDownDeltaThreshold" Text="" />
                                                </td>
                                                <td>
                                                    <dx:ASPxSpinEdit runat="server" ID="seDownDeltaThreshold" />
                                                </td>
                                                <td>
                                                    <dx:ASPxLabel runat="server" ID="lblUpDeltaThreshold" Text="" />
                                                </td>
                                                <td>
                                                    <dx:ASPxSpinEdit runat="server" ID="seUpDeltaThreshold" />
                                                </td>
                                                <td style="margin-left: 5px; text-align: center;">
                                                    <dx:ASPxButton runat="server" ID="btnCreateCorrections" Text="Genera Rettifiche" AutoPostBack="False">
                                                        <ClientSideEvents Click="function (s, e) { cbInsertCorrection.PerformCallback('create'); }" />
                                                    </dx:ASPxButton>
                                                </td>
                                                <td style="margin-left: 5px; text-align: center;">
                                                    <dx:ASPxButton runat="server" ID="btnDeleteCorrections" Text="Elimina Rettifiche" AutoPostBack="False">
                                                        <ClientSideEvents Click="function (s, e) { cbInsertCorrection.PerformCallback('delete'); }" />
                                                    </dx:ASPxButton>
                                                </td>
                                            </tr>
                                        </table>
                                    </dx:LayoutItemNestedControlContainer>
                                </LayoutItemNestedControlCollection>
                            </dx:LayoutItem>
                        </Items>

                        <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
                    </dx:LayoutGroup>
                </Items>
            </dx:ASPxFormLayout>
            <dx:ASPxCallbackPanel runat="server" ID="PnlTimesheetGrids" ClientInstanceName="pnlTimesheetGrids" OnCallback="PnlTimesheetGrids_OnCallback">
                <PanelCollection>
                    <dx:PanelContent ID="PnlTimesheetGridsContent1" runat="server" SupportsDisabledAttribute="True">
                        <div id="timesheetHeaderDiv" style="clear: both; margin-top: 10px">
                            <dx:ASPxComboBox ID="cmbTSMLayout" runat="server" ShowImageInEditBox="true" CssClass="layoutSelector"
                                AutoPostBack="false" DropDownStyle="DropDown" ClientInstanceName="cmbTSMLayout">
                                <ClientSideEvents SelectedIndexChanged="cmbTSMLayout_OnSelectedIndexChanged" />
                            </dx:ASPxComboBox>
                            <dx:ASPxButton ID="btnTSMSaveLayout" runat="server" CssClass="layoutSelector" AutoPostBack="false" UseSubmitBehavior="false">
                                <ClientSideEvents Click="function(s, e) {cpTSMLayout.PerformCallback('save');}" />
                                <Image Url="~/Icons/Save/Save.png">
                                </Image>
                            </dx:ASPxButton>
                            <dx:ASPxButton ID="btnTSMDeleteLayout" runat="server" CssClass="layoutSelector" AutoPostBack="false" UseSubmitBehavior="false">
                                <ClientSideEvents Click="function(s, e) {cpTSMLayout.PerformCallback('delete');}" />
                                <Image Url="~/Icons/Delete/Delete.png">
                                </Image>
                            </dx:ASPxButton>
                            <dx:ASPxButton ID="btnLoad" runat="server" AutoPostBack="False" CssClass="headerButtons" ClientInstanceName="btnLoad" ClientSideEvents-Init="deTimesheet_OnValueChanged" UseSubmitBehavior="false">
                                <Image Url="~/Icons/Load/Load.png">
                                </Image>
                                <ClientSideEvents Click="btnLoad_OnClick" />
                            </dx:ASPxButton>
                            <dx:ASPxButton ID="btnTSMCustomizeColumns" ClientInstanceName="btnTSMCustomizeColumns"
                                CssClass="headerButtons" runat="server" AutoPostBack="false" UseSubmitBehavior="false">
                                <ClientSideEvents Click="btnTSMCustomizeColumns_OnClick" />
                                <Image Url="~/Icons/Customize Column/Customize Column.png">
                                </Image>
                            </dx:ASPxButton>
                            <dx:ASPxButton ID="btnTSMPrintPdf" runat="server" CssClass="headerButtons"
                                AutoPostBack="false" OnClick="btnTSMPrintPdf_Click" UseSubmitBehavior="false">
                                <Image Url="~/Icons/PDF/PDF.png">
                                </Image>
                            </dx:ASPxButton>
                            <dx:ASPxButton ID="btnTSMPrintXlsx" runat="server" CssClass="headerButtons"
                                AutoPostBack="false" OnClick="btnTSMPrintXlsx_Click" UseSubmitBehavior="false">
                                <Image Url="~/Icons/XLSX/XLSX.png">
                                </Image>
                            </dx:ASPxButton>
                            <dx:ASPxDropDownEdit ClientInstanceName="tsOptionsCombobox" ID="TsOptionsCombobox" Width="200px" runat="server" AnimationType="None" ReadOnly="True" Text="Opzioni cartellino"
                                CssClass="headerButtons" OnInit="TsOptionsCombobox_OnInit">
                                <DropDownWindowTemplate> 
                                    <dx:ASPxListBox Width="100%" ID="LbOptionsListBox" ClientInstanceName="lbOptionsListBox" SelectionMode="CheckColumn"
                                        runat="server">
                                        <ClientSideEvents SelectedIndexChanged="OnListBoxSelectionChanged" Init="OnListBoxSelectionChanged" />
                                    </dx:ASPxListBox>
                                </DropDownWindowTemplate>
                            </dx:ASPxDropDownEdit>
                            <dx:ASPxDateEdit ID="deTimesheet" runat="server" ClientInstanceName="deTimesheet" CssClass="headerButtons" EditFormatString="MMM yyyy" DisplayFormatString="MMM yyyy" ShowShadow="False">
                                <ClientSideEvents ValueChanged="deTimesheet_OnValueChanged" DropDown="OndeTimesheet_DropDown" Init="OndeTimesheet_Init" />
                            </dx:ASPxDateEdit>
                            <table class="headerButtons">
                                <tr>
                                    <td>
                                        <dx:ASPxLabel ID="lblPeriodo" runat="server" Text="" />
                                    </td>
                                </tr>
                            </table>
                            <dx:ASPxCheckBox runat="server" ID="ChkShowWeeklyTotals" ClientInstanceName="chkShowWeeklyTotals" ClientSideEvents-CheckedChanged="function (s, e) { TimesheetTotalsChanged(); }"
                                CssClass="headerButtons" >

<ClientSideEvents CheckedChanged="function (s, e) { TimesheetTotalsChanged(); }"></ClientSideEvents>
                            </dx:ASPxCheckBox>

                        </div>
                        <div style="float: left; clear: both; width: 100%">

                            <dx:ASPxPanel runat="server" ID="PnlTimesheetMonthlyTotals" ClientInstanceName="pnlTimesheetMonthlyTotals">
                                <PanelCollection>
                                    <dx:PanelContent ID="PnlTimesheetMonthlyTotalsContent1" runat="server" SupportsDisabledAttribute="True">
                                        <%-- GRIGLIA DI VISUALIZZAZIONE DEL CARTELLINO CON SOLI TOTALI MENSILI --%>
                                        <dx:ASPxGridView ID="gvTimesheet" ClientInstanceName="gvTimesheet" runat="server" Width="100%" AutoGenerateColumns="False" SettingsEditing-Mode="Inline"
                                            OnCustomCallback="TimesheetGridView_CustomCallback"
                                            OnCustomSummaryCalculate="TimesheetGridView_CustomSummaryCalculate"
                                            OnHtmlFooterCellPrepared="TimesheetGridView_HtmlFooterCellPrepared"
                                            OnHtmlDataCellPrepared="TimesheetGridView_HtmlDataCellPrepared"
                                            OnCommandButtonInitialize="TimesheetGridView_OnCommandButtonInitialize"
                                            OnRowValidating="TimesheetGridView_OnRowValidating"
                                            OnRowUpdating="TimesheetGridView_OnRowUpdating"
                                            OnDataBound="TimesheetGridView_OnDataBound" OnInit="TimesheetGridView_OnInit" EnableViewState="false">
                                            <SettingsEditing Mode="Inline"></SettingsEditing>
                                            <Settings ShowHeaderFilterButton="True" ShowHeaderFilterBlankItems="False"></Settings>
                                            <Columns>
                                                <dx:GridViewCommandColumn ShowEditButton="true" VisibleIndex="0" />
                                                <dx:GridViewDataTextColumn FieldName="ID" Visible="false" ShowInCustomizationForm="false" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataComboBoxColumn FieldName="ColId" VisibleIndex="98" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataComboBoxColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantMnemonic" ReadOnly="True" Visible="false" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantDesc" ReadOnly="True" Visible="false" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="ColMnemonic" VisibleIndex="1" ReadOnly="True" GroupIndex="1" SortIndex="1" SortOrder="Ascending" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="ColDesc" Visible="true" FixedStyle="Left" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Justification" VisibleIndex="5" ReadOnly="True" FixedStyle="Left">
                                                    <Settings HeaderFilterMode="CheckedList" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day01" VisibleIndex="6" Width="45px" Caption="01">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day02" VisibleIndex="7" Width="45px" Caption="02">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day03" VisibleIndex="8" Width="45px" Caption="03">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day04" VisibleIndex="9" Width="45px" Caption="04">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day05" VisibleIndex="10" Width="45px" Caption="05">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day06" VisibleIndex="11" Width="45px" Caption="06">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day07" VisibleIndex="12" Width="45px" Caption="07">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day08" VisibleIndex="13" Width="45px" Caption="08">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day09" VisibleIndex="14" Width="45px" Caption="09">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day10" VisibleIndex="15" Width="45px" Caption="10">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day11" VisibleIndex="16" Width="45px" Caption="11">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day12" VisibleIndex="17" Width="45px" Caption="12">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day13" VisibleIndex="18" Width="45px" Caption="13">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day14" VisibleIndex="19" Width="45px" Caption="14">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day15" VisibleIndex="20" Width="45px" Caption="15">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day16" VisibleIndex="21" Width="45px" Caption="16">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day17" VisibleIndex="22" Width="45px" Caption="17">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day18" VisibleIndex="23" Width="45px" Caption="18">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day19" VisibleIndex="24" Width="45px" Caption="19">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day20" VisibleIndex="25" Width="45px" Caption="20">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day21" VisibleIndex="26" Width="45px" Caption="21">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day22" VisibleIndex="27" Width="45px" Caption="22">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day23" VisibleIndex="28" Width="45px" Caption="23">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day24" VisibleIndex="29" Width="45px" Caption="24">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day25" VisibleIndex="30" Width="45px" Caption="25">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day26" VisibleIndex="31" Width="45px" Caption="26">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day27" VisibleIndex="32" Width="45px" Caption="27">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day28" VisibleIndex="33" Width="45px" Caption="28">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day29" VisibleIndex="34" Width="45px" Caption="29">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day30" VisibleIndex="35" Width="45px" Caption="30">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day31" VisibleIndex="36" Width="45px" Caption="31">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Order" ReadOnly="True" Visible="false" SortIndex="1" SortOrder="Ascending">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="LastMonthlyHours" VisibleIndex="38" ReadOnly="True" UnboundType="String">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalHours" VisibleIndex="38" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="CurrentMonthlyHours" VisibleIndex="38" ReadOnly="True" UnboundType="String">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalDays" VisibleIndex="39" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataCheckColumn FieldName="IsFromFreeTimeSheet" VisibleIndex="98" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataCheckColumn>
                                                <dx:GridViewDataTextColumn FieldName="FreeTimeSheetId" VisibleIndex="99" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataComboBoxColumn FieldName="CantId" VisibleIndex="99" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataComboBoxColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantDesc" ReadOnly="True" Visible="false">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                            </Columns>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="LastMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;&#39;&#39;}" ShowInGroupFooterColumn="LastMonthlyHours" Tag="LastMonthlyHours"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="CurrentMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;&#39;&#39;}" ShowInGroupFooterColumn="CurrentMonthlyHours" Tag="CurrentMonthlyHours"></dx:ASPxSummaryItem>
                                            </GroupSummary>
                                            <%--<GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                            </GroupSummary>--%>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="LastMonthlyHours" ShowInGroupFooterColumn="LastMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;''}" SummaryType="Custom" Tag="LastMonthlyHours" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="CurrentMonthlyHours" ShowInGroupFooterColumn="CurrentMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;''}" SummaryType="Custom" Tag="CurrentMonthlyHours" />
                                            </GroupSummary>
                                        </dx:ASPxGridView>
                                    </dx:PanelContent>
                                </PanelCollection>
                            </dx:ASPxPanel>
                            <dx:ASPxPanel runat="server" ID="PnlTimesheetWithWeeklyTotals" ClientInstanceName="pnlTimesheetWithWeeklyTotals" ClientVisible="False">
                                <PanelCollection>
                                    <dx:PanelContent ID="PnlTimesheetWeeklyTotalsContent" runat="server" SupportsDisabledAttribute="True">
                                        <%-- GRIGLIA DI VISUALIZZAZIONE DEL CARTELLINO CON TOTALI MENSILI E SETTIMANALI --%>
                                        <dx:ASPxGridView ID="gvTimesheetWeeklyTotals" ClientInstanceName="gvTimesheetWeeklyTotals" runat="server" Width="100%" AutoGenerateColumns="False" SettingsEditing-Mode="Inline"
                                            OnCustomCallback="TimesheetGridView_CustomCallback"
                                            OnCustomSummaryCalculate="TimesheetGridView_CustomSummaryCalculate"
                                            OnHtmlFooterCellPrepared="TimesheetGridView_HtmlFooterCellPrepared"
                                            OnHtmlDataCellPrepared="TimesheetGridView_HtmlDataCellPrepared"
                                            OnCommandButtonInitialize="TimesheetGridView_OnCommandButtonInitialize"
                                            OnRowValidating="TimesheetGridView_OnRowValidating"
                                            OnRowUpdating="TimesheetGridView_OnRowUpdating"
                                            OnDataBound="TimesheetGridView_OnDataBound" OnInit="TimesheetGridView_OnInit">
                                            <SettingsEditing Mode="Inline"></SettingsEditing>
                                            <Settings ShowHeaderFilterButton="True" ShowHeaderFilterBlankItems="False"></Settings>
                                            <Columns>
                                                <dx:GridViewCommandColumn ShowEditButton="true" VisibleIndex="0" />
                                                <dx:GridViewDataTextColumn FieldName="ID" Visible="false" ShowInCustomizationForm="false" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataComboBoxColumn FieldName="ColId" VisibleIndex="980" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataComboBoxColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantMnemonic" ReadOnly="True" Visible="false" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantDesc" ReadOnly="True" Visible="false" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="ColMnemonic" VisibleIndex="10" ReadOnly="True" GroupIndex="1" SortIndex="1" SortOrder="Ascending" FixedStyle="Left">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="ColDesc" Visible="True" FixedStyle="Left" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Justification" VisibleIndex="50" ReadOnly="True" FixedStyle="Left">
                                                    <Settings HeaderFilterMode="CheckedList" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus7" VisibleIndex="60" Width="45px" Caption="-7">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus6" VisibleIndex="70" Width="45px" Caption="-6">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus5" VisibleIndex="80" Width="45px" Caption="-5">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus4" VisibleIndex="90" Width="45px" Caption="-4">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus3" VisibleIndex="100" Width="45px" Caption="-3">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus2" VisibleIndex="110" Width="45px" Caption="-2">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayMinus1" VisibleIndex="120" Width="45px" Caption="-1">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day01" VisibleIndex="130" Width="45px" Caption="01">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day02" VisibleIndex="140" Width="45px" Caption="02">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day03" VisibleIndex="150" Width="45px" Caption="03">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day04" VisibleIndex="160" Width="45px" Caption="04">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day05" VisibleIndex="170" Width="45px" Caption="05">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day06" VisibleIndex="180" Width="45px" Caption="06">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day07" VisibleIndex="190" Width="45px" Caption="07">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day08" VisibleIndex="200" Width="45px" Caption="08">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day09" VisibleIndex="210" Width="45px" Caption="09">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day10" VisibleIndex="220" Width="45px" Caption="10">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day11" VisibleIndex="230" Width="45px" Caption="11">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day12" VisibleIndex="240" Width="45px" Caption="12">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day13" VisibleIndex="250" Width="45px" Caption="13">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day14" VisibleIndex="260" Width="45px" Caption="14">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day15" VisibleIndex="270" Width="45px" Caption="15">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day16" VisibleIndex="280" Width="45px" Caption="16">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day17" VisibleIndex="290" Width="45px" Caption="17">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day18" VisibleIndex="300" Width="45px" Caption="18">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day19" VisibleIndex="310" Width="45px" Caption="19">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day20" VisibleIndex="320" Width="45px" Caption="20">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day21" VisibleIndex="330" Width="45px" Caption="21">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day22" VisibleIndex="340" Width="45px" Caption="22">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day23" VisibleIndex="350" Width="45px" Caption="23">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day24" VisibleIndex="360" Width="45px" Caption="24">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day25" VisibleIndex="370" Width="45px" Caption="25">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day26" VisibleIndex="380" Width="45px" Caption="26">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day27" VisibleIndex="390" Width="45px" Caption="27">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day28" VisibleIndex="400" Width="45px" Caption="28">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day29" VisibleIndex="410" Width="45px" Caption="29">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day30" VisibleIndex="420" Width="45px" Caption="30">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Day31" VisibleIndex="430" Width="45px" Caption="31">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus1" VisibleIndex="440" Width="45px" Caption="+1">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus2" VisibleIndex="450" Width="45px" Caption="+2">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus3" VisibleIndex="460" Width="45px" Caption="+3">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus4" VisibleIndex="470" Width="45px" Caption="+4">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus5" VisibleIndex="480" Width="45px" Caption="+5">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus6" VisibleIndex="490" Width="45px" Caption="+6">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="DayPlus7" VisibleIndex="500" Width="45px" Caption="+7">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}" MaskSettings-Mask="<0..23g>.<00..99>">
                                                        <MaskSettings Mask="&lt;0..23g&gt;.&lt;00..99&gt;"></MaskSettings>
                                                        <ValidationSettings Display="Dynamic" />
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="Order" ReadOnly="True" Visible="false" SortIndex="1" SortOrder="Ascending">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="LastMonthlyHours" VisibleIndex="510" ReadOnly="True" UnboundType="String">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalHours" VisibleIndex="520" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="CurrentMonthlyHours" VisibleIndex="530" ReadOnly="True" UnboundType="String">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalDays" VisibleIndex="540" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek1" VisibleIndex="540" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek2" VisibleIndex="550" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek3" VisibleIndex="560" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek4" VisibleIndex="570" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek5" VisibleIndex="580" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataTextColumn FieldName="TotalWeek6" VisibleIndex="590" ReadOnly="True">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <PropertiesTextEdit DisplayFormatString="{0:#0.00;-#0.00;''}">
                                                    </PropertiesTextEdit>
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataCheckColumn FieldName="IsFromFreeTimeSheet" VisibleIndex="980" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataCheckColumn>
                                                <dx:GridViewDataTextColumn FieldName="FreeTimeSheetId" VisibleIndex="990" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                </dx:GridViewDataTextColumn>
                                                <dx:GridViewDataComboBoxColumn FieldName="CantId" VisibleIndex="990" ReadOnly="True" Visible="False">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataComboBoxColumn>
                                                <dx:GridViewDataTextColumn FieldName="CantDesc" ReadOnly="True" Visible="false">
                                                    <Settings AllowHeaderFilter="False" />
                                                    <EditFormSettings Visible="False" CaptionLocation="None" />
                                                </dx:GridViewDataTextColumn>
                                            </Columns>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Plan" />
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Ord"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Str"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="StrNot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Just"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Delta"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Delta"></dx:ASPxSummaryItem>
                                                <dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Arrot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Justification" DisplayFormat=" " ShowInGroupFooterColumn="Justification" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus1" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus2" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus3" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus4" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus5" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus6" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayMinus7" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day01" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day01" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day02" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day02" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day03" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day03" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day04" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day04" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day05" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day05" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day06" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day06" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day07" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day07" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day08" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day08" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day09" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day09" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day10" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day10" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day11" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day11" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day12" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day12" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day13" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day13" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day14" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day14" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day15" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day15" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day16" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day16" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day17" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day17" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day18" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day18" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day19" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day19" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day20" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day20" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day21" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day21" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day22" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day22" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day23" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day23" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day24" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day24" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day25" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day25" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day26" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day26" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day27" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day27" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day28" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day28" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day29" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day29" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day30" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day30" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="Day31" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="Day31" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus1" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus2" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus3" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus4" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus5" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus6" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="DayPlus7" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalHours" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalHours" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek1" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek2" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek3" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek4" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek5" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;&#39;-&#39;}" ShowInGroupFooterColumn="TotalWeek6" Tag="Tot"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="LastMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;&#39;&#39;}" ShowInGroupFooterColumn="LastMonthlyHours" Tag="LastMonthlyHours"></dx:ASPxSummaryItem>
<dx:ASPxSummaryItem SummaryType="Custom" FieldName="CurrentMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;&#39;&#39;}" ShowInGroupFooterColumn="CurrentMonthlyHours" Tag="CurrentMonthlyHours"></dx:ASPxSummaryItem>
                                            </GroupSummary>
                                            <%--
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Ord" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Str" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="StrNot" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Just" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Delta" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Arrot" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="Justification" ShowInGroupFooterColumn="Justification" DisplayFormat=" " SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus1" ShowInGroupFooterColumn="DayMinus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus2" ShowInGroupFooterColumn="DayMinus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus3" ShowInGroupFooterColumn="DayMinus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus4" ShowInGroupFooterColumn="DayMinus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus5" ShowInGroupFooterColumn="DayMinus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus6" ShowInGroupFooterColumn="DayMinus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayMinus7" ShowInGroupFooterColumn="DayMinus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day01" ShowInGroupFooterColumn="Day01" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day02" ShowInGroupFooterColumn="Day02" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day03" ShowInGroupFooterColumn="Day03" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day04" ShowInGroupFooterColumn="Day04" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day05" ShowInGroupFooterColumn="Day05" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day06" ShowInGroupFooterColumn="Day06" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day07" ShowInGroupFooterColumn="Day07" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day08" ShowInGroupFooterColumn="Day08" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day09" ShowInGroupFooterColumn="Day09" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day10" ShowInGroupFooterColumn="Day10" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day11" ShowInGroupFooterColumn="Day11" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day12" ShowInGroupFooterColumn="Day12" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day13" ShowInGroupFooterColumn="Day13" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day14" ShowInGroupFooterColumn="Day14" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day15" ShowInGroupFooterColumn="Day15" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day16" ShowInGroupFooterColumn="Day16" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day17" ShowInGroupFooterColumn="Day17" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day18" ShowInGroupFooterColumn="Day18" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day19" ShowInGroupFooterColumn="Day19" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day20" ShowInGroupFooterColumn="Day20" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day21" ShowInGroupFooterColumn="Day21" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day22" ShowInGroupFooterColumn="Day22" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day23" ShowInGroupFooterColumn="Day23" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day24" ShowInGroupFooterColumn="Day24" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day25" ShowInGroupFooterColumn="Day25" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day26" ShowInGroupFooterColumn="Day26" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day27" ShowInGroupFooterColumn="Day27" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day28" ShowInGroupFooterColumn="Day28" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day29" ShowInGroupFooterColumn="Day29" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day30" ShowInGroupFooterColumn="Day30" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="Day31" ShowInGroupFooterColumn="Day31" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus1" ShowInGroupFooterColumn="DayPlus1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus2" ShowInGroupFooterColumn="DayPlus2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus3" ShowInGroupFooterColumn="DayPlus3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus4" ShowInGroupFooterColumn="DayPlus4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus5" ShowInGroupFooterColumn="DayPlus5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus6" ShowInGroupFooterColumn="DayPlus6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="DayPlus7" ShowInGroupFooterColumn="DayPlus7" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalHours" ShowInGroupFooterColumn="TotalHours" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek1" ShowInGroupFooterColumn="TotalWeek1" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek2" ShowInGroupFooterColumn="TotalWeek2" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek3" ShowInGroupFooterColumn="TotalWeek3" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek4" ShowInGroupFooterColumn="TotalWeek4" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek5" ShowInGroupFooterColumn="TotalWeek5" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                                <dx:ASPxSummaryItem FieldName="TotalWeek6" ShowInGroupFooterColumn="TotalWeek6" DisplayFormat="{0:#0.00;-#0.00;'-'}" SummaryType="Custom" Tag="Tot" />
                                            </GroupSummary>
                                                --%>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="LastMonthlyHours" ShowInGroupFooterColumn="LastMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;''}" SummaryType="Custom" Tag="LastMonthlyHours" />
                                            </GroupSummary>
                                            <GroupSummary>
                                                <dx:ASPxSummaryItem FieldName="CurrentMonthlyHours" ShowInGroupFooterColumn="CurrentMonthlyHours" DisplayFormat="{0:#0.00;-#0.00;''}" SummaryType="Custom" Tag="CurrentMonthlyHours" />
                                            </GroupSummary>
                                        </dx:ASPxGridView>
                                    </dx:PanelContent>
                                </PanelCollection>
                            </dx:ASPxPanel>
                        </div>
                    </dx:PanelContent>
                </PanelCollection>
            </dx:ASPxCallbackPanel>
        </dx:PanelContent>
    </PanelCollection>
</dx:ASPxCallbackPanel>
