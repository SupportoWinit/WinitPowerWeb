<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_DistModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_DistModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<% if(DesignMode) {  %><script src="~/Scripts/ASPxScriptIntelliSense.js" type="text/javascript"></script><% }  %>
<script type="text/javascript">
    $(window).on('load', function() {
        if (typeof btnPrintPdf != 'undefined') {
            btnPrintPdf.SetVisible(false);
        }

        if (typeof btnPrintXlsx != 'undefined') {
            btnPrintXlsx.SetVisible(false);
        }
    })
</script>
<table>
    <tr>
<dx:ASPxGridView ID="gvTabDecod" runat="server" AutoGenerateColumns="False" Width="100%">
    <Columns>
        <dx:GridViewDataTextColumn FieldName="Chiave_Tab" VisibleIndex="4" Width="30%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Decodifica_Tab_Desc" VisibleIndex="5" Width="50%">
        </dx:GridViewDataTextColumn>
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvTabDist" runat="server" AutoGenerateColumns="False" Width="100%"                
                OnInit="gvTabDist_Detail_Init"
                OnInitNewRow="gvTabDist_Detail_InitNewRow"
                OnRowValidating="gvTabDist_Detail_RowValidating"
                OnRowInserting="gvTabDist_Detail_RowInserting" 
                OnRowUpdating="gvTabDist_Detail_RowUpdating"
                OnRowDeleting="gvTabDist_Detail_RowDeleting"                  
                OnBeforePerformDataSelect="gvTabDist_Detail_BeforePerformDataSelect">
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
                            <Image Url="../Icons/Edit/Edit.png"/>
                        </EditButton>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Undo/Undo.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataTextColumn FieldName="Tab_Dist_Id" Visible="false" ShowInCustomizationForm="false">
                    </dx:GridViewDataTextColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Arrivo_Tab_Dist" VisibleIndex="50" Width="30%">
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataSpinEditColumn FieldName="KM_Tab_Dist" VisibleIndex="70" Width="5%">
                    </dx:GridViewDataSpinEditColumn>
                    <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tab_Dist" VisibleIndex="80" Width="5%">
                    </dx:GridViewDataSpinEditColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Partenza_Tab_Dist" VisibleIndex="20" Width="30%">
                    </dx:GridViewDataComboBoxColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
        </tr>
    </table>






