<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_EditFormTemplateModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_EditFormTemplateModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView ID="gvTab_EditFormTemplate" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvTab_EditFormTemplate_DataBinding"
    OnInitNewRow="gvTab_EditFormTemplate_InitNewRow"
    OnRowValidating="gvTab_EditFormTemplate_RowValidating"
    OnRowInserting="gvTab_EditFormTemplate_RowInserting"  
    OnRowDeleting="gvTab_EditFormTemplate_RowDeleting" 
    OnRowUpdating="gvTab_EditFormTemplate_RowUpdating">    
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
        <dx:GridViewDataTextColumn FieldName="Tab_EditFormTemplate_Id" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Entity_Tab_EditFormTemplate" ReadOnly="True" VisibleIndex="10"  Width ="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Tabs_Tab_EditFormTemplate" VisibleIndex="20" Width ="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fields_Tab_EditFormTemplate" VisibleIndex="30" Width ="15%">
        </dx:GridViewDataTextColumn>                     
    </Columns>    
</dx:ASPxGridView>
        </tr>
    </table>
