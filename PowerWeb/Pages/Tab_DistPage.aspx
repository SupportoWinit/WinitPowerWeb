<%@ Page Title="PowerWeb - Tabella distanze" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_DistPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_DistPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_DistModule" Src="~/Modules/Tab_DistModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_DistModule runat="server" ID="mdlTab_Dist" />
</asp:Content>
