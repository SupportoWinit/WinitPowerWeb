<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ResourcesModule.ascx.cs" Inherits="PowerWeb.Modules.ResourcesModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvResources" runat="server" AutoGenerateColumns="False" Width="100%"  OnDataBinding="gvResources_DataBinding" 
    OnInitNewRow="gvResources_InitNewRow"
    OnRowValidating="gvResources_RowValidating"
    OnRowInserting="gvResources_RowInserting"
    OnRowUpdating="gvResources_RowUpdating" 
    OnRowDeleting="gvResources_RowDeleting">
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
        <dx:GridViewDataTextColumn FieldName="Resources_Id" Visible="false" >
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Lingue_Id" VisibleIndex="10" Width="10%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Versioni_Id" VisibleIndex="20" Width="10%">
        </dx:GridViewDataComboBoxColumn>                      
        <dx:GridViewDataTextColumn FieldName="ResourceKey" VisibleIndex="30" Width="30%" >
        </dx:GridViewDataTextColumn>           
        <dx:GridViewDataTextColumn FieldName="ResourceValue" VisibleIndex="100" Width="45%">
        </dx:GridViewDataTextColumn>                   
    </Columns>   
</dx:ASPxGridView>
        </tr>
    </table>
