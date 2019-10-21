<%@ Page Title="PowerWeb - Associazione unità portatili" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Pru_ColPage.aspx.cs" Inherits="PowerWeb.Pages.Pru_ColPage" %>

<%@ Register TagPrefix="pw" TagName="Pru_ColModule" Src="~/Modules/Pru_ColModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Pru_ColModule runat="server" ID="mdlPru_ColModule" />
</asp:Content>
