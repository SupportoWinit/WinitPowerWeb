<%@ Page Title="PowerWeb - Inizializzazione Monte Minuti" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="MonteMinutiPage.aspx.cs" Inherits="PowerWeb.Pages.MonteMinutiPage" %>

<%@ Register TagPrefix="pw" TagName="MonteMinutiModule" Src="~/Modules/MonteMinutiModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:MonteMinutiModule runat="server" ID="mdlMonteMinuti" />
</asp:Content>
