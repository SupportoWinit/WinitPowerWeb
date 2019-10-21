<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ImportErrorsModule.ascx.cs"
    Inherits="PowerWeb.Modules.ImportErrorsModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxTabControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxClasses" TagPrefix="dx" %>
<dx:ASPxGridView ID="gvErrori_Import" runat="server" AutoGenerateColumns="False" Width="100%"
    KeyFieldName="Err_Id" >
    <Columns>
        <dx:GridViewDataTextColumn FieldName="Err_Id" VisibleIndex="0" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Matr_Pru_Fru" VisibleIndex="1" Width="250" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Error_Message" VisibleIndex="2" Width="600">
        </dx:GridViewDataTextColumn>
    </Columns>
</dx:ASPxGridView>