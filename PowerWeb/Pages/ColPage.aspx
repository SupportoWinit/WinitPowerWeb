<%@ Page Title="PowerWeb - Collaboratori" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" EnableSessionState="ReadOnly"
    CodeBehind="ColPage.aspx.cs" Inherits="PowerWeb.Pages.ColPage" %>

<%@ Register TagPrefix="pw" TagName="ColModule" Src="~/Modules/ColModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ColModule runat="server" ID="mdlCol" />
</asp:Content>