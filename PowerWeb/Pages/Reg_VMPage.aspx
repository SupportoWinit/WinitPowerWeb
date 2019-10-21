<%@ Page Title="PowerWeb - Manutenzione timbrature" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="Reg_VMPage.aspx.cs" Inherits="PowerWeb.Pages.Reg_VMPage" %>
<%@ MasterType  virtualPath="~/GridMasterPage.master"%>
<%@ Register TagPrefix="pw" TagName="Reg_VMModule" Src="~/Modules/Reg_VMModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Reg_VMModule runat="server" ID="mdlReg_VMModule" />
</asp:Content>
