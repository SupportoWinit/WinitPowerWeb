<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Utenti_FilPage.aspx.cs" Inherits="PowerWeb.Pages.Utenti_FilPage" %>
<%@ Register TagPrefix="pw" TagName="Utenti_FilModule" Src="~/Modules/Utenti_FilModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Utenti_FilModule runat="server" ID="mdlUtenti_FilModule" />
</asp:Content>
