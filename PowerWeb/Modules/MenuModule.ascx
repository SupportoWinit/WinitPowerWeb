<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MenuModule.ascx.cs"
    Inherits="PowerWeb.Modules.MenuModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.ASPxTreeList.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxTreeList" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<script type="text/javascript">

    function cbxMenuType_OnSelectedIndexChanged(s, e) {
        var t = cbxMenuType.GetSelectedIndex();
        if (t === -1) {
            cpTree.PerformCallback('save');
        } else {
            cpTree.PerformCallback('index|' + t);
        }
    }


    function tlMenu_OnEndDragNode(s, e) {

        if (e.htmlEvent.shiftKey) {
            e.cancel = true;
            var key = s.GetNodeKeyByRow(e.targetElement);
            s.PerformCustomCallback('reorder:' + e.nodeKey + ':' + key);
        }
    }

    function cbIncludeFunz_OnCheckedChanged(s, e) {
        var currentCB = ASPxClientCheckBox.Cast(s);

        var res = currentCB.GetChecked() ? 1 : 0;

        cpTree.PerformCallback('hide|' + res);

    }

    function getCurrentPage() {
        var sPath = window.location.pathname;
        var sPage = sPath.substring(sPath.lastIndexOf('/') + 1);
        return sPage;
    }

    function getCurrentPageNoExtNoPage() {
        var sPage = getCurrentPage();
        return sPage.substr(0, sPage.lastIndexOf('Page'));
    }


    function getBaseURL() {
        return location.protocol + "//" + location.hostname + (location.port && ":" + location.port) + "/";
    }

    function redirectOnHelpPage() {
        window.open(getBaseURL() + 'HelpPages/PAGES/_' + getCurrentPageNoExtNoPage() + '_PAGE.html', '_blank');
    }

    function OnCustomButtonClick(s, e) {

        var currentTreeList = ASPxClientTreeList.Cast(s);

        if (e.buttonID == 'delete') {
            var key = e.nodeKey;
            currentTreeList.PerformCallback('delete|' + key);
        }
    }

</script>
<dx:ASPxCallbackPanel ID="cpTree" runat="server" Width="100%" OnCallback="cpTree_Callback" ClientInstanceName="cpTree">
    <PanelCollection>
        <dx:PanelContent ID="PanelContent2" runat="server" SupportsDisabledAttribute="True">
            <dx:ASPxComboBox ID="cbxMenuType" runat="server" ShowImageInEditBox="true" CssClass="layoutSelector"
                AutoPostBack="false" DropDownStyle="DropDown" ClientInstanceName="cbxMenuType">
                <ClientSideEvents SelectedIndexChanged="cbxMenuType_OnSelectedIndexChanged" />
            </dx:ASPxComboBox>
            <dx:ASPxButton ID="btnSaveLayout" runat="server" CssClass="layoutSelector" AutoPostBack="false" UseSubmitBehavior="false">
                <ClientSideEvents Click="function(s, e) {cpTree.PerformCallback('add');}" />
                <Image Url="~/Icons/Add/Add.png">
                </Image>
            </dx:ASPxButton>
            <dx:ASPxButton UseSubmitBehavior="false" ID="btnHelp" runat="server" CssClass="headerButtons" AutoPostBack="false">
                <ClientSideEvents Click="function(s, e) {redirectOnHelpPage();}" />
                <Image Url="~/Icons/Help/Help.png">
                </Image>
            </dx:ASPxButton>
            <dx:ASPxButton ID="btnDeleteLayout" runat="server" CssClass="layoutSelector" AutoPostBack="false" UseSubmitBehavior="false">
                <ClientSideEvents Click="function(s, e) {cpTree.PerformCallback('delete|' + cbxMenuType.GetSelectedIndex());}" />
                <Image Url="~/Icons/Delete/Delete.png">
                </Image>
            </dx:ASPxButton>
            <dx:ASPxCheckBox ID="cbMenùStandard" runat="server" ClientInstanceName="cbMenùStandard" Text="Menù Standard">
            </dx:ASPxCheckBox>
            <dx:ASPxCheckBox ID="cbIncludeFunz" runat="server" ClientInstanceName="cbIncludeFunz" CssClass="headerButtons" Text="Includi Funzioni">
                <ClientSideEvents CheckedChanged="cbIncludeFunz_OnCheckedChanged" />
            </dx:ASPxCheckBox>
            <dx:ASPxTreeList Width="100%" ID="tlMenu" runat="server" AutoGenerateColumns="False" KeyFieldName="Id" ParentFieldName="ParentId" ClientSideEvents-CustomButtonClick="OnCustomButtonClick"
                OnHtmlRowPrepared="tlMenu_HtmlRowPrepared"
                OnNodeDeleting="tlMenu_NodeDeleting"
                OnNodeInserting="tlMenu_NodeInserting"
                OnNodeUpdating="tlMenu_NodeUpdating"
                OnProcessDragNode="tlMenu_ProcessDragNode" ClientSideEvents-EndDragNode="tlMenu_OnEndDragNode" OnCustomCallback="tlMenu_CustomCallback" OnCommandColumnButtonInitialize="tlMenu_CommandColumnButtonInitialize">
                <Columns>
                    <dx:TreeListTextColumn FieldName="Id" ShowInCustomizationForm="True" Visible="False"
                        VisibleIndex="0">
                    </dx:TreeListTextColumn>
                    <dx:TreeListTextColumn FieldName="Caption" ShowInCustomizationForm="True"
                        VisibleIndex="1">
                    </dx:TreeListTextColumn>
                    <dx:TreeListCommandColumn ShowInCustomizationForm="True" VisibleIndex="4" ButtonType="Image" Width="100px" CellStyle-HorizontalAlign="Center">
                        <NewButton Visible="True">
                            <Image Url="~/Icons/Add/Add.png">
                            </Image>
                        </NewButton>
                        <EditButton Visible="True">
                            <Image Url="~/Icons/Edit/Edit.png">
                            </Image>
                        </EditButton>
<%--                        <DeleteButton Visible="True">
                            <Image Url="~/Icons/Delete/Delete.png">
                            </Image>
                        </DeleteButton>--%>
                        <UpdateButton>
                            <Image Url="~/Icons/Check/Check.png">
                            </Image>
                        </UpdateButton>
                        <CancelButton>
                            <Image Url="~/Icons/Undo/Undo.png">
                            </Image>
                        </CancelButton>
                        <CellStyle>
                            <Paddings Padding="5px" PaddingLeft="5px" PaddingRight="5px" />
                        </CellStyle>
                        <CustomButtons>
                            <dx:TreeListCommandColumnCustomButton ID="delete">
                                <Image ToolTip="Delete" Url="~/Icons/Delete/Delete.png">
                                </Image>
                            </dx:TreeListCommandColumnCustomButton>
                        </CustomButtons>
                    </dx:TreeListCommandColumn>
                </Columns>
                <SettingsBehavior AllowSort="False" />
                <SettingsEditing AllowNodeDragDrop="True" AllowRecursiveDelete="true" ConfirmDelete="false" />
            </dx:ASPxTreeList>
        </dx:PanelContent>
    </PanelCollection>
</dx:ASPxCallbackPanel>
