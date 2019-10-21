<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_CheckImpModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_CheckImpModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<dx:ASPxGridView ID="gvTabCheckImp" runat="server" AutoGenerateColumns="False" Width="100%"   
    OnRowDeleting="gvTabCheckImp_RowDeleting">     
    <Columns>    
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">                                        
        <DeleteButton Visible="True">
            <Image Url="../Icons/Delete/Delete.png" />
        </DeleteButton>                          
        </dx:GridViewCommandColumn> 
        <dx:GridViewDataTextColumn FieldName="Tab_Chk_Imp_Id" Visible="false">
        </dx:GridViewDataTextColumn>               
        <dx:GridViewDataTextColumn FieldName="Nome_Tabella_Tab_Check_Imp" VisibleIndex="10" Width="25%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Chiave_Record_Tab_Check_Imp" VisibleIndex="20" Width ="60%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Stato_Record_Tab_Check_Imp" VisibleIndex="20" Width ="5%">
        </dx:GridViewDataTextColumn>
    </Columns>   
</dx:ASPxGridView>






