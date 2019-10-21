<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="VersioniModule.ascx.cs"
    Inherits="PowerWeb.Modules.VersioniModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvVersioni" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvVersioni_DataBinding"
    OnInitNewRow="gvVersioni_InitNewRow"
    OnRowValidating="gvVersioni_RowValidating"
    OnRowInserting="gvVersioni_RowInserting"
    OnRowUpdating="gvVersioni_RowUpdating"
    OnRowDeleting="gvVersioni_RowDeleting">
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
                <Image Url="../Icons/Edit/Edit.png" />
            </EditButton>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn>       
        <dx:GridViewDataTextColumn FieldName="Nome_Versioni" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Versioni_Id" VisibleIndex="10" Width="70%">
        </dx:GridViewDataComboBoxColumn>                
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Versioni" VisibleIndex="50" Width="10%" ReadOnly="True">           
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Versioni" VisibleIndex="60" Width="15%" ReadOnly="True">
             <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Versioni" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>       
    </Columns>
</dx:ASPxGridView>
        </tr>
    </table>

