<%@ Page Title="PowerWeb - Associazione unità fisse" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Fru_CantPage.aspx.cs" Inherits="PowerWeb.Pages.Fru_CantPage" %>

<%@ Register TagPrefix="pw" TagName="Fru_CantModule" Src="~/Modules/Fru_CantModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Fru_CantModule runat="server" ID="mdlFru_CantModule" />
</asp:Content>
