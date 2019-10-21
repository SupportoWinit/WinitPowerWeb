<%@ Page Title="PowerWeb - Errori import" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="ImportErrorsPage.aspx.cs" Inherits="PowerWeb.Pages.ImportErrorsPage" %>

<%@ Register TagPrefix="pw" TagName="ImportErrorsModule" Src="~/Modules/ImportErrorsModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ImportErrorsModule runat="server" ID="mdlImportErrorsModule" />
</asp:Content>
