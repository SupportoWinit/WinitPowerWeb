<%@ Page Title="PowerWeb - Comuni" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_ComuniPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_ComuniPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_ComuniModule" Src="~/Modules/Tab_ComuniModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_ComuniModule runat="server" ID="mdlTab_Comuni" />
</asp:Content>
