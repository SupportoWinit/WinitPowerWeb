<%@ Page Title="PowerWeb - Unità fisse" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="FruPage.aspx.cs" Inherits="PowerWeb.Pages.FruPage" %>

<%@ Register TagPrefix="pw" TagName="FruModule" Src="~/Modules/FruModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:FruModule runat="server" ID="mdlFru" />
</asp:Content>
