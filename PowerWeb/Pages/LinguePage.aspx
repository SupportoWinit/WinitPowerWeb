<%@ Page Title="PowerWeb - Lingue" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="LinguePage.aspx.cs" Inherits="PowerWeb.Pages.LinguePage" %>

<%@ Register TagPrefix="pw" TagName="LingueModule" Src="~/Modules/LingueModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:LingueModule runat="server" ID="mdlLingueModule" />
</asp:Content>
