<%@ Page Title="PowerWeb - Tabella festivi" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_FestiviPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_FestiviPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_FestiviModule" Src="~/Modules/Tab_FestiviModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_FestiviModule runat="server" ID="mdlTab_Festivi" />
</asp:Content>
