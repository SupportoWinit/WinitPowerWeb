<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FilModule.ascx.cs" 
    Inherits="PowerWeb.Modules.FilModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvFil" runat="server" AutoGenerateColumns="False" Width="100%" EnableViewState="false" OnDataBinding="gvFil_DataBinding"
    OnInitNewRow="gvFil_InitNewRow"
    OnRowValidating="gvFil_RowValidating"
    OnRowInserting="gvFil_RowInserting"
    OnRowUpdating="gvFil_RowUpdating"
    OnRowDeleting="gvFil_RowDeleting"> 
    <Columns>
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
            <CustomButtons>                           
                <dx:GridViewCommandColumnCustomButton ID="add">
                    <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="addClone">
                    <Image ToolTip="Favorites" Url="../Icons/Add/Add.png" />
                </dx:GridViewCommandColumnCustomButton>
                                <dx:GridViewCommandColumnCustomButton ID="delete">
                    <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="view">
                    <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                </dx:GridViewCommandColumnCustomButton>
            </CustomButtons>
            <EditButton Visible="True">
                <Image Url="../Icons/Edit/Edit.png" />
            </EditButton>           
            <CancelButton Visible="true">
                <Image Url="../Icons/Undo/Undo.png" />
            </CancelButton>
            <UpdateButton Visible="true">
                <Image Url="../Icons/Check/Check.png" />
            </UpdateButton>
            <ClearFilterButton Visible="True">
            </ClearFilterButton>
        </dx:GridViewCommandColumn>    
        <dx:GridViewDataTextColumn FieldName="Fil_Id" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Fil" VisibleIndex="1" Width ="10%" >
        </dx:GridViewDataTextColumn>               
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Fil" VisibleIndex="80" ReadOnly = "true" Width ="10%" >
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Fil" VisibleIndex="90" ReadOnly = "true" Width = "15%">
              <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataTextColumn FieldName="Descrizione_Fil" VisibleIndex="20" Width = "20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Fil" VisibleIndex="110" Width = "5%">
        </dx:GridViewDataCheckColumn> 
        <dx:GridViewDataTextColumn FieldName="Note_Fil" VisibleIndex="100" Width="30%">
        </dx:GridViewDataTextColumn>                
    </Columns>    
</dx:ASPxGridView>
        </tr>
    </table>
