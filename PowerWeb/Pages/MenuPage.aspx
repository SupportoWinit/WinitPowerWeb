<%@ Page Title="PowerWeb - Menu" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="MenuPage.aspx.cs" Inherits="PowerWeb.Pages.MenuPage" %>

<%@ Register TagPrefix="pw" TagName="MenuModule" Src="~/Modules/MenuModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:MenuModule runat="server" ID="mdlMenuModule" />
</asp:Content>
