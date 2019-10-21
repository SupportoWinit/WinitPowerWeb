<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="VersioniPage.aspx.cs" Inherits="PowerWeb.Pages.VersioniPage" %>

<%@ Register TagPrefix="pw" TagName="VersioniModule" Src="~/Modules/VersioniModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:VersioniModule runat="server" ID="mdlVersioni" />
</asp:Content>
