<%@ Page Title="PowerWeb - Registrazioni errate" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="Reg_VErrPage.aspx.cs" Inherits="PowerWeb.Pages.Reg_VErrPage" %>
<%@ Register TagPrefix="pw" TagName="Reg_VErrModule" Src="~/Modules/RegV_ErrModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Reg_VErrModule runat="server" ID="mdlRegVErrModule" />
</asp:Content>
