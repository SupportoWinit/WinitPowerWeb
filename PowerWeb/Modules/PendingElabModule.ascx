<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PendingElabModule.ascx.cs"
    Inherits="PowerWeb.Modules.PendingElabModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvPendingElab" runat="server" AutoGenerateColumns="False" Width="100%"   
    OnRowDeleting="gvPendingElab_RowDeleting">     
    <Columns>    
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">                                        
        <DeleteButton Visible="True">
            <Image Url="../Icons/Delete/Delete.png" />
        </DeleteButton>                          
        </dx:GridViewCommandColumn> 
        <dx:GridViewDataTextColumn FieldName="PendingElab_Id" Visible="false">
        </dx:GridViewDataTextColumn>               
         <dx:GridViewDataDateColumn FieldName="ElaborateDate_PendingElab" VisibleIndex="10" Width="15%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="FromDate_PendingElab" VisibleIndex="20" Width="15%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="ToDate_PendingElab" VisibleIndex="30" Width ="15%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Fru_Id" VisibleIndex="40" Width ="15%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Pru_Id" VisibleIndex="50" Width ="15%">
        </dx:GridViewDataComboBoxColumn>
        
    </Columns>   
</dx:ASPxGridView>
        </tr>
    </table>






