<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Aut_FerPerModule.ascx.cs" Inherits="PowerWeb.Modules.Aut_FerPerModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallbackPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>

<script type="text/javascript">
    jQuery(window).on('load', function () {
        jQuery("#gridDiv").css("display", "none");
        hideBatchMode();
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
            checkboxBatch.SetEnabled(false);
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

<div style="clear: both; float: none; border: 1px solid darkgrey; padding: 5px">
    <table>
        <tr>
            <td>
                <dx:ASPxLabel ID="StateLabel" runat="server" Text="Selezione Ferie/Permessi: " Font-Bold="true" />
            </td>
            <td>
                <dx:ASPxDropDownEdit ClientInstanceName="FerPerComboBox" ID="FerPerComboBox" Width="380px" runat="server" AnimationType="None" ReadOnly="True">
                    <DropDownWindowTemplate>
                        <dx:ASPxListBox Width="100%" ID="FerPerListBox" ClientInstanceName="FerPerListBox" SelectionMode="CheckColumn"
                            runat="server">
                            <Items>
                                <dx:ListEditItem Text="99 - Seleziona tutto" Value="99" Selected="True" />
                                <dx:ListEditItem Text="0 - Ferie" Value="0" />
                                <dx:ListEditItem Text="1 - Permessi" Value="1" />
                            </Items>
                            <ClientSideEvents SelectedIndexChanged="OnListBoxSelectionChanged" Init="OnListBoxSelectionChanged" />
                        </dx:ASPxListBox>
                    </DropDownWindowTemplate>
                </dx:ASPxDropDownEdit>
            </td>
            <td>
                <dx:ASPxLabel ID="SearchDateFromLabel" runat="server" Text="Dal: " Font-Bold="true" />
            </td>
            <td>
                <dx:ASPxDateEdit ID="SearchDateFrom" ClientInstanceName="SearchDateFrom" runat="server" Width="100px" />
            </td>
            <td>
                <dx:ASPxLabel ID="SearchDateToLabel" runat="server" Text="Al: " Font-Bold="true" />
            </td>
            <td>
                <dx:ASPxDateEdit ID="SearchDateTo" ClientInstanceName="SearchDateTo" runat="server" Width="100px" />
            </td>
            <td>
                <dx:ASPxButton Text="Applica" ClientInstanceName="btnApplyPreSelection" ID="btnApplyPreSelection" runat="server" AutoPostBack="false">
                    <ClientSideEvents Click="function(s, e) {applyPreFilter();}" />
                </dx:ASPxButton>
            </td>
        </tr>
        <tr>
            <td>
                <dx:ASPxLabel runat="server" ID="LblColPreFilter" ClientInstanceName="lblColPreFilter" Font-Bold="True" />
            </td>
            <td>
                <dx:ASPxComboBox runat="server" ID="CmbColPreFilter" Width="380px" ClientInstanceName="cmbColPreFilter"></dx:ASPxComboBox>
            </td>
            <td colspan="2">
                <dx:ASPxLabel runat="server" ID="LblIfEmptyAll" ClientInstanceName="lblIfEmptyAll" Font-Size="80%" />
            </td>
            <td></td>
            <td></td>
            <td></td>
        </tr>
    </table>
    <dx:ASPxDateEdit ID="BreakRegDate" ClientInstanceName="BreakRegDate" runat="server" ClientVisible="false" />
</div>
<div id="gridDiv" style="margin-top: 20px; margin-bottom: 20px;">
    <dx:ASPxCallbackPanel ID="filterPanel" runat="server" Width="100%" ClientInstanceName="filterPanel"
        OnCallback="filterPanel_Callback" ClientSideEvents-EndCallback="filterPanel_OnEndCallback" ClientSideEvents-Init="function(s, e) { hidebuttons(); hideBatchMode(); }">

        <ClientSideEvents EndCallback="filterPanel_OnEndCallback" Init="function(s, e) { hidebuttons(); hideBatchMode(); }"></ClientSideEvents>

        <PanelCollection>
            <dx:PanelContent ID="filterPanelContent" runat="server" SupportsDisabledAttribute="True">
                <div style="clear: both; float: left;">
                    <table>
                        <tr>
                            <td style="width: 40px;">
                                <dx:ASPxLabel ID="lblCol_Id" runat="server" Text="Coll: " Font-Bold="true" />
                            </td>
                            <td>
                                <dx:ASPxComboBox runat="server" ID="cmbCol_Id" ClientInstanceName="cmbCol_Id" Width="380px">
                                    <ClientSideEvents SelectedIndexChanged="function(s, e) { doCallbackFilterPanel('colIdChanged'); }" />
                                </dx:ASPxComboBox>
                            </td>
                            <td>
                                <dx:ASPxButton ClientInstanceName="btnGoPreviousCol" ID="btnGoPreviousCol" runat="server" AutoPostBack="false" RenderMode="Link">
                                    <Image Url="~/Icons/Previous/Previous.png"></Image>
                                    <ClientSideEvents Click="function(s, e) { doCallbackFilterPanel('goToPreviousCol');}" />
                                </dx:ASPxButton>
                            </td>
                            <td>
                                <dx:ASPxButton ClientInstanceName="btnGoNextCol" ID="btnGoNextCol" runat="server" AutoPostBack="false" RenderMode="Link">
                                    <Image Url="~/Icons/Next/Next.png"></Image>
                                    <ClientSideEvents Click="function(s, e) { doCallbackFilterPanel('goToNextCol'); }" />
                                </dx:ASPxButton>
                            </td>
                            <td>
                                <div style="f  loat: left">
                                    <dx:ASPxButton Text="Proposta Chiusura" ClientInstanceName="btnBtnPropostaChiusura" ID="BtnPropostaChiusura" runat="server" AutoPostBack="false" Visible="False" Enabled="False">
                                        <ClientSideEvents Click="function(s, e) { doCallbackFilterPanel('automaticCorrection#' + cbChiusuraAllSelected.GetChecked()); }" />
                                    </dx:ASPxButton>
                                </div>
                                <div style="float: right">
                                    <dx:ASPxCheckBox runat="server" ID="CbChiusuraAllSelected" ClientInstanceName="cbChiusuraAllSelected" Text="Tutte le errate" Visible="false" />
                                </div>
                            </td>
                        </tr>
                    </table>
                    <table>
                        <tr>
                            <td style="width: 40px;">
                                <dx:ASPxLabel ID="lblData_Reg" runat="server" Text="Data: " Font-Bold="true" />
                            </td>
                            <td>
                                <dx:ASPxComboBox runat="server" ID="cmbData_Reg" ClientInstanceName="cmbData_Reg" Width="100px">
                                    <ClientSideEvents SelectedIndexChanged="function(s, e) { doCallbackFilterPanel('dataRegChanged'); }" />
                                </dx:ASPxComboBox>
                            </td>
                            <td>
                                <dx:ASPxButton ClientInstanceName="btnGoPreviousDate" ID="btnGoPreviousDate" runat="server" AutoPostBack="false" RenderMode="Link">
                                    <Image Url="~/Icons/Previous/Previous.png"></Image>
                                    <ClientSideEvents Click="function(s, e) { gotoPreviousDate(); }" />
                                </dx:ASPxButton>
                            </td>
                            <td>
                                <dx:ASPxButton ClientInstanceName="btnGoNextDate" ID="btnGoNextDate" runat="server" AutoPostBack="false" RenderMode="Link">
                                    <Image Url="~/Icons/Next/Next.png"></Image>
                                    <ClientSideEvents Click="function(s, e) { gotoNextDate();}" />

                                </dx:ASPxButton>
                            </td>
                            <td>
                                <dx:ASPxLabel runat="server" ID="LblColDataSource" />
                            </td>
                        </tr>
                    </table>
                </div>
                <div style="margin-top: 10px; margin-bottom: 10px; float: right;">
                    <dx:ASPxCheckBox ID="cbIncludeAllDayReg" runat="server" ClientInstanceName="cbIncludeAllDayReg" CssClass="headerButtons" Text="Visualizza giorno completo" Checked="True">
                        <ClientSideEvents CheckedChanged="function(s, e) { doCallbackFilterPanel('includeReg');}" />
                    </dx:ASPxCheckBox>
                    <dx:ASPxCheckBox ID="cbIncludeActivity" runat="server" ClientInstanceName="cbIncludeActivity" CssClass="headerButtons" Text="Includi Attività">
                        <ClientSideEvents CheckedChanged="function(s, e) { doCallbackFilterPanel('includeReg');}" />
                    </dx:ASPxCheckBox>
                    <dx:ASPxCheckBox ID="cbIncludePass" runat="server" ClientInstanceName="cbIncludePass" CssClass="headerButtons" Text="Includi Passaggi">

                        <ClientSideEvents CheckedChanged="function(s, e) { doCallbackFilterPanel('includeReg');}" />
                    </dx:ASPxCheckBox>
                    <dx:ASPxCheckBox ID="cbIncludeTrips" runat="server" ClientInstanceName="cbIncludeTrips" CssClass="headerButtons" Text="Includi Viaggi">
                        <ClientSideEvents CheckedChanged="function(s, e) { doCallbackFilterPanel('includeReg');}" />
                    </dx:ASPxCheckBox>
                    <dx:ASPxCheckBox ID="cbIncludeBlocked" runat="server" ClientInstanceName="cbIncludeBlocked" CssClass="headerButtons" Text="Includi Bloccate">
                        <ClientSideEvents CheckedChanged="function(s, e) { doCallbackFilterPanel('includeReg');}" />
                    </dx:ASPxCheckBox>
                </div>

                <div style="float: left; width: 100%;">
                    <dx:ASPxGridView ID="gvAutFerPerEdit" runat="server" AutoGenerateColumns="False" Width="100%"
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
                                    <dx:GridViewCommandColumnCustomButton ID="delete">
                                        <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
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
