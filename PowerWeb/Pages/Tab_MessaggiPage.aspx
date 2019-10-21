<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_MessaggiPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_MessaggiPage" %>

<%@ Register TagPrefix="pw" TagName="Tab_MessaggiModule" Src="~/Modules/Tab_MessaggiModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_MessaggiModule runat="server" ID="mdlTab_Messaggi" />
</asp:Content>
