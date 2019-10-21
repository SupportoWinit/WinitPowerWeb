<%@ Page Title="PowerWeb - Resources" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="ResourcesPage.aspx.cs" Inherits="PowerWeb.Pages.ResourcesPage" %>

<%@ Register TagPrefix="pw" TagName="ResourcesModule" Src="~/Modules/ResourcesModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ResourcesModule runat="server" ID="mdlResourcesModule" />
</asp:Content>
