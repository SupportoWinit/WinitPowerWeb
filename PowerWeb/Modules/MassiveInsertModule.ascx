<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MassiveInsertModule.ascx.cs" Inherits="PowerWeb.Modules.MassiveInsertModule" %>

<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxLoadingPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxFormLayout" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>

<script type="text/javascript">

    function doCallback(parameter) {
        var currGrid = ASPxClientGridView.Cast(grid);
        loadingPanel.Show();
        currGrid.UpdateEdit();
        currGrid.PerformCallback(parameter);
        currGrid.CancelEdit();

    }

    function resetSpinEdit() {
        var currSpinEdit = ASPxClientSpinEdit.Cast(seNumberOfDate);
        currSpinEdit.SetValue(0);
    }

    function Grid_BatchEditConfirmShowing(s, e) {
        e.cancel = true;
    }


    function gvMassiveInsertEdit_OnEndCallback(s, e) {
        if (s.cpHidePanel == true)
            loadingPanel.Hide();
        if (s.cpErrorString != null)
            DisplayDialogError("Power", s.cpErrorString);

    }

    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }
</script>

<dx:ASPxFormLayout ID="flStandardInsert" runat="server" Width="80%">
    <Items>
        <dx:LayoutGroup Caption="Inserimento standard" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td>
                                        <dx:ASPxButton ID="btnAdd" ClientInstanceName="btnAdd" runat="server" AutoPostBack="False" CssClass="headerButtons" Paddings-Padding="10px" EnableTheming="true" RenderMode="Link">
                                            <Image Url="~/Icons/Add/Add.png">
                                            </Image>
                                            <ClientSideEvents Click="function(s, e) { doCallback('addSingle'); } " />
                                        </dx:ASPxButton>
                                    </td>
                                    <td>
                                        <dx:ASPxButton ID="btnAddMultiRow" ClientInstanceName="btnAddMultiRow" runat="server" AutoPostBack="False" CssClass="headerButtons" Paddings-Padding="10px" RenderMode="Link">
                                            <Image Url="~/Icons/Edit/EditMulti.png">
                                            </Image>
                                            <ClientSideEvents Click="function(s, e) { doCallback('addMulti'); } " />
                                        </dx:ASPxButton>
                                    </td>
                                    <td>
                                        <dx:ASPxSpinEdit ID="seNumberOfDate" ClientInstanceName="seNumberOfDate" runat="server" AutoPostBack="false" MinValue="0" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel ID="lblNumberOfDay" ClientInstanceName="lblNumberOfDay" runat="server" Font-Size="14">
                                        </dx:ASPxLabel>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
        <dx:LayoutGroup Caption="Inserimento da tabella orari" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblSelColId" ClientInstanceName="lblSelColId" Text="Collaboratore:" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbSelColId" ClientInstanceName="cmbSelCodId" Width="200" DropDownWidth="600" AutoResizeWithContainer="False" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblDateStart" ClientInstanceName="lblDateStart" Text="Data inizio" />
                                    </td>
                                    <td>
                                        <dx:ASPxDateEdit runat="server" ID="DeDateStart" ClientInstanceName="deDateStart" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblDateEnd" ClientInstanceName="lblDateEnd" Text="Data fine" />
                                    </td>
                                    <td>
                                        <dx:ASPxDateEdit runat="server" ID="DeDateEnd" ClientInstanceName="deDateEnd" />
                                    </td>
                                    <td>
                                        <dx:ASPxButton ID="BtnAddFromTimesheet" ClientInstanceName="btnAddFromtimeSheet" runat="server" AutoPostBack="False" CssClass="headerButtons"
                                            Paddings-Padding="10px" EnableTheming="true" RenderMode="Link">
                                            <Image Url="~/Icons/Add/Add.png">
                                            </Image>
                                            <ClientSideEvents Click="function(s, e) { doCallback('insertFromTimeSheet'); } " />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblSelCantId" ClientInstanceName="lblSelCantId" Text="Cantiere:" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbSelCantId" ClientInstanceName="cmbSelCantId" Width="200" DropDownWidth="600" AutoResizeWithContainer="False" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblSelMotivazione" ClientInstanceName="lblSelMotivazione" Text="Motivazione:" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbSelMotivazione" ClientInstanceName="cmbSelMotivazione" Width="200" DropDownWidth="600" AutoResizeWithContainer="False" />
                                    </td>
                                    <td></td>
                                    <td></td>
                                    <td></td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>
<table>
    <tr>
        <dx:ASPxGridView ID="gvMassiveInsertEdit" ClientInstanceName="grid" runat="server" AutoGenerateColumns="False" Width="100%" OnAfterPerformCallback="gvMassiveInsertEdit_OnAfterPerformCallback"
            OnCustomCallback="gvMassiveInsertEdit_CustomCallback" OnDataBinding="gvMassiveInsertEdit_DataBinding" OnAutoFilterCellEditorInitialize="gvMassiveInsertEdit_OnAutoFilterCellEditorInitialize" OnBatchUpdate="gvMassiveInsertEdit_BatchUpdate"
            OnCommandButtonInitialize="gvMassiveInsertEdit_CommandButtonInitialize" ClientSideEvents-BatchEditConfirmShowing="Grid_BatchEditConfirmShowing" OnRowUpdating="gvMassiveInsertEdit_RowUpdating" OnRowDeleting="gvMassiveInsertEdit_OnRowDeleting">
            <SettingsLoadingPanel Mode="Disabled"></SettingsLoadingPanel>
            <ClientSideEvents EndCallback="function(s, e) { gvMassiveInsertEdit_OnEndCallback(s,e);}" />
            <Columns>
                <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image" ShowDeleteButton="True">
                    <DeleteButton>
                        <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                    </DeleteButton>
                </dx:GridViewCommandColumn>
                <dx:GridViewDataComboBoxColumn FieldName="RegE" VisibleIndex="0" Visible="false" />
                <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="2" Width="25%" />
                <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" VisibleIndex="2" Width="25%" />
                <dx:GridViewDataDateColumn FieldName="Data_Reg" VisibleIndex="4" Width="10%">
                    <PropertiesDateEdit EditFormat="Date" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Ora_Fis_E" VisibleIndex="4" Width="10%">
                    <PropertiesDateEdit EditFormat="Time" DisplayFormatString="t">
                        <TimeSectionProperties Visible="true" />
                    </PropertiesDateEdit>
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Ora_Fis_U" VisibleIndex="5" Width="10%">
                    <PropertiesDateEdit EditFormat="Time" DisplayFormatString="t">
                        <TimeSectionProperties Visible="true" />
                    </PropertiesDateEdit>
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Motivazione_Reg_Id" VisibleIndex="6" Width="10%" />
                <dx:GridViewDataDateColumn FieldName="Durata_Fis_HH_C" VisibleIndex="7" Width="10%">
                    <PropertiesDateEdit EditFormat="Time" DisplayFormatString="t">
                        <TimeSectionProperties Visible="true" />
                    </PropertiesDateEdit>
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataCheckColumn FieldName="IsOnlyDuration" VisibleIndex="8" Width="5%" />
                <dx:GridViewDataTextColumn FieldName="Note_Reg" VisibleIndex="9" Width="5%" />
            </Columns>
            <SettingsBehavior ColumnResizeMode="Control" AllowGroup="false" AllowDragDrop="false" AllowSort="false" />
            <Settings HorizontalScrollBarMode="Auto" ShowFilterRow="false" ShowFooter="false" />
            <SettingsPager Mode="ShowAllRecords" />
            <SettingsEditing Mode="Batch" />
        </dx:ASPxGridView>
    </tr>
</table>
<div style="clear: both; margin-top: 10px; float: left">
    <dx:ASPxButton ID="btnUndo" ClientInstanceName="btnUndo" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
        <Image Url="~/Icons/Undo/Undo.png">
        </Image>
        <ClientSideEvents Click="function(s, e) { doCallback('undo'); resetSpinEdit(); } " />
    </dx:ASPxButton>
    <dx:ASPxButton ID="btnUpdateMemory" ClientInstanceName="btnUpdateMemory" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
        <Image Url="~/Icons/Refresh/Refresh.png">
        </Image>
        <ClientSideEvents Click="function(s, e) { doCallback('updateToMemory'); }" />

        <Paddings Padding="10px"></Paddings>
    </dx:ASPxButton>
    <dx:ASPxButton ID="btnUpdate" ClientInstanceName="btnUpdate" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" EnableDefaultAppearance="false" Paddings-Padding="10px" RenderMode="Link">
        <Image Url="~/Icons/Check/Check.png">
        </Image>
        <ClientSideEvents Click="function(s, e) {doCallback('elaborate'); resetSpinEdit(); } " />
    </dx:ASPxButton>
</div>

<dx:ASPxLoadingPanel ID="loadingPanel" runat="server" ClientInstanceName="loadingPanel" Modal="True">
</dx:ASPxLoadingPanel>

<dx:ASPxDateEdit runat="server" ID="__ReferenceDateEdit" ClientVisible="false"></dx:ASPxDateEdit>







