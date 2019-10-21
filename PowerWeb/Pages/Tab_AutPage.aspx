<%@ Page Title="PowerWeb - TabAut" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_AutPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_AutPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_AutModule" Src="~/Modules/Tab_AutModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_AutModule runat="server" ID="mdlTab_Aut" />
</asp:Content>
