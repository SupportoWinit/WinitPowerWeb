<%@ Page Title="PowerWeb" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Utenti_RespPage.aspx.cs" Inherits="PowerWeb.Pages.Utenti_RespPage" %>
<%@ Register TagPrefix="pw" TagName="Utenti_RespModule" Src="~/Modules/Utenti_RespModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Utenti_RespModule runat="server" ID="mdlUtenti_RespModule" />
</asp:Content>
