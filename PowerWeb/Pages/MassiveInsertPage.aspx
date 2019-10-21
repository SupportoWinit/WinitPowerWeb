<%@ Page Title="PowerWeb - Inserimento massivo" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="MassiveInsertPage.aspx.cs" Inherits="PowerWeb.Pages.MassiveInsertPage" %>
<%@ Register TagPrefix="pw1" TagName="MassiveInsertModule" Src="~/Modules/MassiveInsertModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw1:MassiveInsertModule runat="server" ID="mdlMassiveInsertModule" />
</asp:Content>
