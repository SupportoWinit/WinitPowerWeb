<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Aut_FerPerModule.ascx.cs" Inherits="PowerWeb.Modules.Aut_FerPerModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallbackPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>

<script type="text/javascript">
    jQuery(window).on('load', function () {
        jQuery("#gridDiv").css("display", "none");
        hideBatchMode();
        gridDiv.style.display = 'block';
        doCallbackFilterPanel('PreFilterSelected');
    });

    var isEditPending = false;

    function gotoNextDate() {
        //var cmbDate = ASPxClientComboBox.Cast(cmbData_Reg);
        var cmbDate = ASPxClientComboBox.Cast(cmbData_Reg);

        if (cmbDate.GetSelectedIndex() < cmbDate.GetItemCount()) {
            cmbDate.SetSelectedIndex(cmbDate.GetSelectedIndex() + 1);
            doCallbackFilterPanel('dataRegChanged');
        }

    }

    function hideBatchMode() {
        try {
            var checkboxBatch = ASPxClientCheckBox.Cast(cbBatchMode);
            checkboxBatch.SetEnabled(true);
        } catch (e) {
            // silenziamento dell'errore in caso di mancata presenza del checkbox per la modifica diretta
        }

    }

    function hidebuttons() {

        jQuery('#bottomBtnDiv').css("display", "none");
    }

    function gotoPreviousDate() {
        //var cmbDate = ASPxClientComboBox.Cast(cmbData_Reg);
        var cmbDate = ASPxClientComboBox.Cast(cmbData_Reg);

        if (cmbDate.GetSelectedIndex() > 0) {
            cmbDate.SetSelectedIndex(cmbDate.GetSelectedIndex() - 1);
            doCallbackFilterPanel('dataRegChanged');
        }
    }

    function addEditRow() {
        var currGrid = ASPxClientGridView.Cast(grid);

        if (isEditPending)
            currGrid.PerformCallback('empty');
        else currGrid.PerformCallback('add');

        setEditGridVisible(true);

        isEditPending = false;

    }

    function addEditRows() {

        var currGrid = ASPxClientGridView.Cast(grid);

        if (isEditPending)
            currGrid.PerformCallback('empty');
        else currGrid.PerformCallback('adds');

        setEditGridVisible(true);

        isEditPending = false;
    }

    function doCallbackFilterPanel(parameter) {
        filterPanel.PerformCallback(parameter);
    }

    function doCallbackGrid(parameter) {
        var currGrid = ASPxClientGridView.Cast(grid);

        if (parameter == 'updateToMemory' || parameter == 'elaborate') {
            currGrid.UpdateEdit();
            currGrid.PerformCallback(parameter);
            currGrid.CancelEdit();
        }
        else {
            currGrid.PerformCallback(parameter);
        }
    }

    function Grid_BatchEditConfirmShowing(s, e) {
        e.cancel = true;
    }

    function OnCustomAcceptClick(s, e) {

        var currentGrid = ASPxClientGridView.Cast(s);
        var currGrid = ASPxClientGridView.Cast(grid);

        if (e.buttonID == 'accept') {
            var currKey = currentGrid.GetRowKey(e.visibleIndex);
            currGrid.PerformCallback('accept|' + currKey);
        } else if (e.buttonID == 'deleteRequest') {
            var currKey = currentGrid.GetRowKey(e.visibleIndex);
            currGrid.PerformCallback('deleteRequest|' + currKey);
        }
    }

    function gvAutFerPerEdit_OnEndCallback(s, e) {
        if (s.cpCallBackParameter == 'elaborate') {
            if (s.cpErrorString != null) {
                DisplayDialogError("Power", s.cpErrorString);
                s.cpErrorString = null;
            } else {
                doCallbackFilterPanel('endElaborate');
                s.cpCallBackParameter = undefined;
            }
        }
    }

    function filterPanel_OnEndCallback(s, e) {
        if (s.cpCallBackParameter == 'undo') {
            gridDiv.style.display = 'none';
            s.cpCallBackParameter = undefined;
        }
        else if (s.cpCallBackParameter == 'nextCol' || s.cpCallBackParameter == 'previousCol') {
            // in caso stia ritornando da un ricalcolo di prossimo/precedente collaboratore allora devo ricalcolare anche il valore
            // della data
            doCallbackFilterPanel('colIdChanged');
            s.cpCallBackParameter = undefined;
        }
        else if (filterPanel.cpCorrectionMessage != '' && typeof filterPanel.cpCorrectionMessage != 'undefined') { // se al termine dell'elaborazione di correzione ho un messaggio da visualizzare
            // visualizzo il messaggio
            DisplayDialogInfo("Power", filterPanel.cpCorrectionMessage);

            // svuoto la variabile utilizzata per la visualizzazione
            filterPanel.cpCorrectionMessage = '';
        }
    }

    // funzione scatenata all'applicazione del prefiltro
    function applyPreFilter() {

        // variabile che mi segnala se proseguire o meno
        var prosegui = false;

        // per proseguire con l'elaborazione è necessario che sia stato selezionato almeno uno stato, che le date
        // siano valorizzate correttamente e che la data dal sia maggiore della data blocco
        var errList = ASPxClientListBox.Cast(FerPerListBox);
        var selectedItems = errList.GetSelectedItems();

        if (GetSelectedItemsText(selectedItems) == '' || GetSelectedItemsText(selectedItems) == undefined) {
            alert('Selezionare gli errori da ricercare!');
        } else {
            var searchDateFrom = ASPxClientDateEdit.Cast(SearchDateFrom).GetDate();
            var searchDateTo = ASPxClientDateEdit.Cast(SearchDateTo).GetDate();
            var blockDate = ASPxClientDateEdit.Cast(BreakRegDate).GetDate();

            if (searchDateFrom == undefined || searchDateTo == undefined) {
                alert('Selezionare il periodo da ricercare!');
            } else {
                if (searchDateFrom > searchDateTo) {
                    alert('Selezionare date coerenti!');
                } else {
                    if (searchDateFrom < blockDate) {
                        alert('Data dal deve essere maggiore di data blocco: ' + blockDate.toLocaleDateString());
                    } else {
                        prosegui = true;
                    }
                }
            }
        }

        // DAL, AL VALORIZZATE E COERENTI
        if (prosegui) {
            gridDiv.style.display = 'block';
            doCallbackFilterPanel('PreFilterSelected');
        }
    }

    //#region GESTIONE LISTBOX ERRORI
    var textSeparator = ";";
    function OnListBoxSelectionChanged(listBox, args) {
        if (FerPerListBox.GetItem(0).selected) {
            FerPerListBox.UnselectAll();
            FerPerListBox.SetSelectedIndex(0);
        }

        UpdateText();
    }

    function UpdateSelectAllItemState() {
        IsAllSelected() ? FerPerListBox.SelectIndices([0]) : FerPerListBox.UnselectIndices([0]);
    }
    function IsAllSelected() {
        var selectedDataItemCount = FerPerListBox.GetItemCount() - (FerPerListBox.GetItem(0).selected ? 0 : 1);
        return FerPerListBox.GetSelectedItems().length == selectedDataItemCount;
    }
    function UpdateText() {
        var selectedItems = FerPerListBox.GetSelectedItems();
        FerPerComboBox.SetText(GetSelectedItemsText(selectedItems));
    }
    function SynchronizeListBoxValues(dropDown, args) {
        FerPerListBox.UnselectAll();
        var texts = dropDown.GetText().split(textSeparator);
        var values = GetValuesByTexts(texts);
        FerPerListBox.SelectValues(values);
        UpdateSelectAllItemState();
        UpdateText(); // for remove non-existing texts
    }
    function GetSelectedItemsText(items) {
        var texts = [];
        for (var i = 0; i < items.length; i++)
            texts.push(items[i].text);
        return texts.join(textSeparator);
    }

    //#endregion

    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }

    function gvAutFerPerEdit_BatchEditStartEditing(s, e) {
        try{
            if (cmbData_Reg.GetValue() == e.rowValues[s.GetColumnByField("Data_Reg").index].text){
                e.cancel = false;
            }
            else{
                e.cancel = true;
            }
        }
        catch (e) {
            e.cancel = true;
        }
        

        
    }
</script>

<div id="gridDiv" style="margin-top: 20px; margin-bottom: 20px;">
    <dx:ASPxCallbackPanel ID="filterPanel" runat="server" Width="100%" ClientInstanceName="filterPanel"
        OnCallback="filterPanel_Callback" ClientSideEvents-EndCallback="filterPanel_OnEndCallback" ClientSideEvents-Init="function(s, e) { hidebuttons(); hideBatchMode(); }">

        <ClientSideEvents EndCallback="filterPanel_OnEndCallback" Init="function(s, e) { hidebuttons(); hideBatchMode(); }"></ClientSideEvents>

        <PanelCollection>
            <dx:PanelContent ID="filterPanelContent" runat="server" SupportsDisabledAttribute="True">

                <div style="float: left; width: 100%;">
                    <dx:ASPxGridView ID="gvAutFerPerEdit" ClientInstanceName="gvAutFerPerEdit" runat="server" AutoGenerateColumns="False" Width="100%"
                        OnCustomCallback="gvAutFerPerEdit_CustomCallback" OnDataBinding="gvAutFerPerEdit_DataBinding" OnAfterPerformCallback="gvAutFerPerEdit_AfterPerformCallback"
                        OnInit="gvAutFerPerEdit_OnInit" OnRowUpdating="gvAutFerPerEdit_OnRowUpdating" OnRowDeleting="gvAutFerPerEdit_OnRowDeleting"
                        OnRowInserting="gvAutFerPerEdit_OnRowInserting" OnBatchUpdate="gvAutFerPerEdit_BatchUpdate" ClientSideEvents-BatchEditConfirmShowing="Grid_BatchEditConfirmShowing"
                        OnCommandButtonInitialize="gvAutFerPerEdit_OnCommandButtonInitialize"
                        ClientSideEvents-EndCallback="gvAutFerPerEdit_OnEndCallback">

                        <ClientSideEvents BatchEditConfirmShowing="Grid_BatchEditConfirmShowing" EndCallback="gvAutFerPerEdit_OnEndCallback" BatchEditStartEditing="gvAutFerPerEdit_BatchEditStartEditing"></ClientSideEvents>

                        <Columns>
                            <dx:GridViewCommandColumn Visible="True" VisibleIndex="0" Width="100px" ButtonType="Image" ShowEditButton="True">
                                <CustomButtons>
                                    <dx:GridViewCommandColumnCustomButton ID="add">
                                        <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                                    </dx:GridViewCommandColumnCustomButton>
                                    <dx:GridViewCommandColumnCustomButton ID="deleteRequest">
                                        <Image ToolTip="deleteRequest" Url="../Icons/Delete/Delete.png" />
                                    </dx:GridViewCommandColumnCustomButton>
                                    <dx:GridViewCommandColumnCustomButton ID="accept">
                                        <Image ToolTip="Accept" Url="../Icons/Check/Check.png" />
                                    </dx:GridViewCommandColumnCustomButton>
                                </CustomButtons>
                                <EditButton>
                                    <Image ToolTip="Edit" Url="../Icons/Edit/Edit.png" />
                                </EditButton>
                                <UpdateButton>
                                    <Image ToolTip="Update" Url="../Icons/Check/Check.png" />
                                </UpdateButton>
                                <CancelButton>
                                    <Image ToolTip="Cancel" Url="../Icons/Undo/Undo.png" />
                                </CancelButton>
                            </dx:GridViewCommandColumn>
                            <dx:GridViewDataTextColumn FieldName="RegE" ReadOnly="True" VisibleIndex="0" Visible="false">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataTextColumn>
                            <dx:GridViewDataTextColumn FieldName="RegU" ReadOnly="True" VisibleIndex="0" Visible="false">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataTextColumn>
                            <dx:GridViewDataTextColumn FieldName="TmpNewId" ReadOnly="True" VisibleIndex="0" Visible="False">
                            </dx:GridViewDataTextColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" VisibleIndex="2" Width="30%">
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataDateColumn FieldName="Data_Ora_Fis_E" VisibleIndex="4" SortIndex="0" SortOrder="Ascending" Width="10%">
                                <PropertiesDateEdit DisplayFormatString="t" EditFormat="Time" />
                            </dx:GridViewDataDateColumn>
                            <dx:GridViewDataDateColumn FieldName="Data_Ora_Fis_U" VisibleIndex="5" Width="10%">
                                <PropertiesDateEdit DisplayFormatString="t" EditFormat="Time" />
                            </dx:GridViewDataDateColumn>
                            <dx:GridViewDataCheckColumn FieldName="IsUTimeSameDayE" VisibleIndex="6" Width="5%">
                            </dx:GridViewDataCheckColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Registrazione_Tipo_Reg" VisibleIndex="7" Width="10%" ReadOnly="True">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Registrazione_Stato_Reg" VisibleIndex="8" Width="10%" ReadOnly="True">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Motivazione_Reg_Id" VisibleIndex="9" Width="15%">
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataCheckColumn FieldName="Registrazione_Bloccata" VisibleIndex="10" Width="5%" ReadOnly="True">
                            </dx:GridViewDataCheckColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Tipo_Modifica" VisibleIndex="0" Visible="False" Width="0%" ReadOnly="True">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataComboBoxColumn FieldName="Fru_Id" VisibleIndex="10" Width="5%" ReadOnly="True">
                                <EditFormSettings Visible="False" />
                            </dx:GridViewDataComboBoxColumn>
                            <dx:GridViewDataTextColumn FieldName="EntrataEU" VisibleIndex="11" Visible="False" Width="0%"/>
                            <dx:GridViewDataTextColumn FieldName="UscitaEU" VisibleIndex="12" Visible="False" Width="0%"/>
                            <dx:GridViewDataDateColumn FieldName="Data_Reg" VisibleIndex="13" Visible="False" Width="10%" ReadOnly="True"/>
                            <dx:GridViewDataDateColumn FieldName="Note_Reg" VisibleIndex="14" Visible="False" Width="10%" ReadOnly="True"/>
                            <dx:GridViewDataDateColumn FieldName="Durata_Fis" VisibleIndex="15" Visible="False" Width="10%" ReadOnly="True"/>
                            <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="16" Width="30%">
                            </dx:GridViewDataComboBoxColumn>
                        </Columns>
                        <SettingsBehavior ColumnResizeMode="Control" AllowGroup="false" />
                        <Settings HorizontalScrollBarMode="Auto" ShowFilterRow="false" ShowFooter="false" />
                        <SettingsPager Mode="ShowAllRecords" />
                        <SettingsEditing Mode="Batch" />
                    </dx:ASPxGridView>
                </div>

                <div style="clear: both; margin-top: 10px; float: left">
                    <table>
                        <tr>
                            <td>
                                <dx:ASPxButton ID="btnUpdateErr" ClientInstanceName="btnUpdateErr" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
                                    <Image Url="~/Icons/Check/Check.png">
                                    </Image>
                                    <ClientSideEvents Click="function(s, e) { doCallbackGrid('elaborate'); }" />

                                    <Paddings Padding="10px"></Paddings>
                                </dx:ASPxButton>
                            </td>
                            <td>
                                <dx:ASPxButton ID="btnUpdateMemory" ClientInstanceName="btnUpdateMemory" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
                                    <Image Url="~/Icons/Refresh/Refresh.png">
                                    </Image>
                                    <ClientSideEvents Click="function(s, e) { doCallbackGrid('updateToMemory');}" />

                                    <Paddings Padding="10px"></Paddings>
                                </dx:ASPxButton>
                            </td>
                            <td>
                                <dx:ASPxButton ID="btnUndoErr" ClientInstanceName="btnUndoErr" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
                                    <Image Url="~/Icons/Undo/Undo.png">
                                    </Image>
                                    <ClientSideEvents Click="function(s, e) { doCallbackFilterPanel('undo');}" />

                                    <Paddings Padding="10px"></Paddings>
                                </dx:ASPxButton>
                            </td>
                        </tr>
                    </table>
                </div>
            </dx:PanelContent>
        </PanelCollection>
    </dx:ASPxCallbackPanel>

</div>



<dx:ASPxDateEdit runat="server" ID="__ReferenceDateEdit" ClientVisible="false">
</dx:ASPxDateEdit>
