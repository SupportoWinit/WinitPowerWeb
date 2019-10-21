<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Fil_UtentiPage.aspx.cs" Inherits="PowerWeb.Pages.Fil_UtentiPage" %>
<%@ Register TagPrefix="pw" TagName="Fil_UtentiModule" Src="~/Modules/Fil_UtentiModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Fil_UtentiModule runat="server" ID="mdlFil_UtentiModule" />
</asp:Content>
