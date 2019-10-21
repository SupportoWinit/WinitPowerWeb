<%@ Page Title="PowerWeb - Tabella decodifiche" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_DecodPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_DecodPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_DecodModule" Src="~/Modules/Tab_DecodModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_DecodModule runat="server" ID="mdlTab_Decod" />
</asp:Content>
