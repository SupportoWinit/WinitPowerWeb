<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_ProvPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_ProvPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_ProvModule" Src="~/Modules/Tab_ProvModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_ProvModule runat="server" ID="mdlTab_Prov" />
</asp:Content>