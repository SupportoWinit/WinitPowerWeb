<%@ Page Title="PowerWeb - Orari" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="Tab_OrariPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_OrariPage" %>
<%@ Register TagPrefix="pw" TagName="Tab_OrariModule" Src="~/Modules/Tab_OrariModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_OrariModule runat="server" ID="mdlTab_OrariModule" />
</asp:Content>