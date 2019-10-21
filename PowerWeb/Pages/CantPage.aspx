<%@ Page Title="PowerWeb - Cantieri" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="CantPage.aspx.cs" Inherits="PowerWeb.Pages.CantPage" %>

<%@ Register TagPrefix="pw" TagName="CantModule" Src="~/Modules/CantModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:CantModule runat="server" ID="mdlCant" />
</asp:Content>