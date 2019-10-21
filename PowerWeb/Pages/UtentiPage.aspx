<%@ Page Title="PowerWeb - Utenti" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="UtentiPage.aspx.cs" Inherits="PowerWeb.Pages.UtentiPage" %>

<%@ Register TagPrefix="pw" TagName="UtentiModule" Src="~/Modules/UtentiModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:UtentiModule runat="server" ID="mdlUtenti" />
</asp:Content>
