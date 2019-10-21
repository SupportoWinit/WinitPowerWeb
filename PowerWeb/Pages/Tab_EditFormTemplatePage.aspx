<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Tab_EditFormTemplatePage.aspx.cs" Inherits="PowerWeb.Pages.Tab_EditFormTemplatePage" %>

<%@ Register TagPrefix="pw" TagName="Tab_EditFormTemplateModule" Src="~/Modules/Tab_EditFormTemplateModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Tab_EditFormTemplateModule runat="server" ID="mdlTab_EditFormTemplate" />
</asp:Content>
