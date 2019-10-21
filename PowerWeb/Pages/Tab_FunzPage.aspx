<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_FunzPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_FunzPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_FunzModule" Src="~/Modules/Tab_FunzModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_FunzModule runat="server" ID="mdlTab_Funz" />
</asp:Content>
