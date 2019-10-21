<%@ Page Title="PowerWeb - Errori importazioni" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_Check_ImpPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_Check_ImpPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_CheckImpModule" Src="~/Modules/Tab_CheckImpModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_CheckImpModule runat="server" ID="mdlTab_CheckImp" />
</asp:Content>
