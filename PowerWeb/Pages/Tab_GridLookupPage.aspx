<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_GridLookupPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_GridLookupPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_GridLookUpModule" Src="~/Modules/Tab_GridLookUpModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_GridLookUpModule runat="server" ID="mdlTab_GridLookUp" />
</asp:Content>
