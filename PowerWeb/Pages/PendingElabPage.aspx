<%@ Page Title="PowerWeb - Elaborazioni pendenti" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="PendingElabPage.aspx.cs" Inherits="PowerWeb.Pages.PendingElabPage" %>

<%@ Register TagPrefix="pw" TagName="PendingElabModule" Src="~/Modules/PendingElabModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:PendingElabModule runat="server" ID="mdlPendingElabModule" />
</asp:Content>
