<%@ Page Title="PowerWeb - Associazione unità portatili" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Col_PruPage.aspx.cs" Inherits="PowerWeb.Pages.Col_PruPage" %>

<%@ Register TagPrefix="pw" TagName="Col_PruModule" Src="~/Modules/Col_PruModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Col_PruModule runat="server" ID="mdlCol_PruModule" />
</asp:Content>
