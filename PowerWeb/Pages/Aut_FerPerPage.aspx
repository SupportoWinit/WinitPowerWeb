<%@ Page Title="PowerWeb - Autorizzazione Ferie Permessi" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="Aut_FerPerPage.aspx.cs" Inherits="PowerWeb.Pages.Aut_FerPerPage" %>
<%@ Register TagPrefix="pw" TagName="Aut_FerPerModule" Src="~/Modules/Aut_FerPerModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Aut_FerPerModule runat="server" ID="mdlAut_FerPerPage" />
</asp:Content>
