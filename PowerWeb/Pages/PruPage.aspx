<%@ Page Title="PowerWeb - Unità portatili" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="PruPage.aspx.cs" Inherits="PowerWeb.Pages.PruPage" %>

<%@ Register TagPrefix="pw" TagName="PruModule" Src="~/Modules/PruModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:PruModule runat="server" ID="mdlPru" />
</asp:Content>
