<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LingueModule.ascx.cs" 
    Inherits="PowerWeb.Modules.LingueModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView ID="gvLingue" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvLingue_DataBinding"
    OnInitNewRow="gvLingue_InitNewRow"
    OnRowValidating="gvLingue_RowValidating"
    OnRowInserting="gvLingue_RowInserting"
    OnRowUpdating="gvLingue_RowUpdating"
    OnRowDeleting="gvLingue_RowDeleting"> 
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
        <dx:GridViewDataTextColumn FieldName="Lingue_Id" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Lingue" VisibleIndex = "40" ReadOnly="True" Width="10%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Lingue" VisibleIndex = "50" ReadOnly="True" Width="15%">
            <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Lingue" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="Sigla_Lingue" VisibleIndex="10" Width ="10%" >
        </dx:GridViewDataTextColumn>                               
        <dx:GridViewDataTextColumn FieldName="Nome_Lingue" VisibleIndex="20" Width = "50%">
        </dx:GridViewDataTextColumn>                       
    </Columns>    
</dx:ASPxGridView>
        </tr>
    </table>
