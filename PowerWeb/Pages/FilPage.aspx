<%@ Page Title="PowerWeb - Filiali" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="FilPage.aspx.cs" Inherits="PowerWeb.Pages.FilPage" %>

<%@ Register TagPrefix="pw" TagName="FilModule" Src="~/Modules/FilModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:FilModule runat="server" ID="mdlFilModule" />
</asp:Content>
