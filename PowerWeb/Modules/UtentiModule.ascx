<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UtentiModule.ascx.cs"
    Inherits="PowerWeb.Modules.UtentiModule" %>
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
<dx:ASPxGridView ID="gvUsers" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvUtenti_DataBinding"
    OnInitNewRow="gvUsers_InitNewRow"
    OnRowValidating="gvUsers_RowValidating"
    OnRowInserting="gvUsers_RowInserting"
    OnRowUpdating="gvUsers_RowUpdating"
    OnRowDeleting="gvUsers_RowDeleting">
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
        <dx:GridViewDataColumn FieldName="Utenti_Id" VisibleIndex="0" Width="10%" Visible="false">
        </dx:GridViewDataColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Utente" VisibleIndex="1" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="100" Width="15%">
        </dx:GridViewDataComboBoxColumn>                
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Utente" VisibleIndex="50" Width="10%" ReadOnly="True">           
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Utente" VisibleIndex="60" Width="15%" ReadOnly="True">
             <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Disabilitazione_Utente" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Fil_Inclusive" VisibleIndex="43" Width="7%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Lingue_Id" VisibleIndex="40" Width="7%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Versioni_Id" VisibleIndex="40" Width="7%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Liv_Utente" VisibleIndex="20" CellStyle-HorizontalAlign="Center" Width="5%" EditFormSettings-Visible="False">
            <PropertiesTextEdit MaxLength="2" NullDisplayText="Da 0 a 10">
                <MaskSettings Mask="<0..99>" />
            </PropertiesTextEdit>
            <EditCellStyle HorizontalAlign="Center"></EditCellStyle>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Liv_Utente_Edit" VisibleIndex="21" CellStyle-HorizontalAlign="Center" Width="5%" Visible ="false" EditFormSettings-Visible="true">
            <PropertiesTextEdit MaxLength="2" NullDisplayText="Da 0 a 10">
                <MaskSettings Mask="<0..99>" />
            </PropertiesTextEdit>
            <EditCellStyle HorizontalAlign="Center"></EditCellStyle>
        </dx:GridViewDataTextColumn>         
        <dx:GridViewDataComboBoxColumn FieldName="Menu_Tipo_Id" VisibleIndex="30" Width="10%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Utente" VisibleIndex="120" Width="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="PasswordHash_Utente" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Password" Visible="False">
            <PropertiesTextEdit Password="True" />
            <EditFormSettings Visible="True" />
        </dx:GridViewDataTextColumn>
      <dx:GridViewDataCheckColumn FieldName="Resp_Inclusive" VisibleIndex="46" Width="7%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="SaltKey_Utente" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Cli_Id" VisibleIndex="130" Visible="False">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName ="SecretAnswer" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName ="SecretQuestion" Visible="False">
        </dx:GridViewDataTextColumn>
    </Columns>
</dx:ASPxGridView>
   </tr>
         </table>
