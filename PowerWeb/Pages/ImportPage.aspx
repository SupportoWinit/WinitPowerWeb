<%@ Page Title="PowerWeb - Import da esterno" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="ImportPage.aspx.cs" Inherits="PowerWeb.Pages.ImportPage" EnableSessionState="ReadOnly" %>

<%@ Register TagPrefix="pw" TagName="ImportModule" Src="~/Modules/ImportModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ImportModule runat="server" ID="mdlImportModule" />
</asp:Content>
