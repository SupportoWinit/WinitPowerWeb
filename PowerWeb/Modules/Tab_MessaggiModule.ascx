<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_MessaggiModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_MessaggiModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>

<dx:ASPxGridView ID="gvMessages" runat="server" AutoGenerateColumns="False" Width="100%"
    OnRowDeleting="gvMessages_RowDeleting">
    <Columns>
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
            <CustomButtons>
                <dx:GridViewCommandColumnCustomButton ID="delete">
                    <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="view">
                    <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                </dx:GridViewCommandColumnCustomButton>
            </CustomButtons>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn>       
        <dx:GridViewDataComboBoxColumn FieldName="Applicazione_Tab_Messaggi_Id" VisibleIndex="10" Width="10%" ReadOnly="true" >
        </dx:GridViewDataComboBoxColumn> 
        <dx:GridViewDataDateColumn FieldName="Data_Tab_Messaggi" VisibleIndex="20" Width="10%" ReadOnly="true" PropertiesDateEdit-DropDownButton-Enabled ="false"> 
            <PropertiesDateEdit EditFormat="Date">
            </PropertiesDateEdit> 
        </dx:GridViewDataDateColumn>                                                                                     
        <dx:GridViewDataComboBoxColumn FieldName="Funzione_Tab_Messaggi_Id" VisibleIndex="30" Width="20%" ReadOnly="false" >
        </dx:GridViewDataComboBoxColumn> 
        <dx:GridViewDataTextColumn FieldName="Testo_Tab_Messaggi" VisibleIndex="40" Width="50%" ReadOnly="false" >
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Utente_Id" VisibleIndex="40" Width="5%" ReadOnly="false" >
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Ora_Elab_Tab_Messaggi" VisibleIndex="20" Width="5%" ReadOnly="true" PropertiesDateEdit-DropDownButton-Enabled ="false">
            <PropertiesDateEdit EditFormat="DateTime"></PropertiesDateEdit>
            <Settings GroupInterval="Value"></Settings>
        </dx:GridViewDataDateColumn> 
    </Columns>
</dx:ASPxGridView>