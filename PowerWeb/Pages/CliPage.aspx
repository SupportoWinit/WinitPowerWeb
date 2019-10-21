<%@ Page Title="PowerWeb - Clienti" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="CliPage.aspx.cs" Inherits="PowerWeb.Pages.CliPage" %>

<%@ Register TagPrefix="pw" TagName="CliModule" Src="~/Modules/CliModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:CliModule runat="server" ID="mdlCli" />
</asp:Content>
