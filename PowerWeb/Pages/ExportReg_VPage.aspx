<%@ Page Title="PowerWeb - Esportazione registrazioni" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="ExportReg_VPage.aspx.cs" Inherits="PowerWeb.Pages.ExportReg_VPage" %>
<%@ Register TagPrefix="pw" TagName="ExportReg_VModule" Src="~/Modules/ExportReg_VModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ExportReg_VModule runat="server" ID="mdlExportReg_VModule" />
</asp:Content>
