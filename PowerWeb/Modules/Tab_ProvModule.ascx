<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_ProvModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_ProvModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView ID="gvTabProv" runat="server" AutoGenerateColumns="False" Width="100%"     
    OnInitNewRow="gvTabProv_InitNewRow"
    OnRowValidating="gvTabProv_RowValidating"
    OnRowInserting="gvTabProv_RowInserting"
    OnRowUpdating="gvTabProv_RowUpdating"
    OnRowDeleting="gvTabProv_RowDeleting"
    OnDataBinding="gvTabProv_DataBinding">
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
        <dx:GridViewDataTextColumn FieldName="Tab_Prov_Id" Visible="false">
        </dx:GridViewDataTextColumn>              
        <dx:GridViewDataTextColumn FieldName="Sigla_Prov" VisibleIndex="10" Width ="10%" >
        </dx:GridViewDataTextColumn>                               
        <dx:GridViewDataTextColumn FieldName="Descrizione_Prov" VisibleIndex="20" Width = "80%">
        </dx:GridViewDataTextColumn>                       
    </Columns>    
</dx:ASPxGridView>
    </tr>
        </table>
