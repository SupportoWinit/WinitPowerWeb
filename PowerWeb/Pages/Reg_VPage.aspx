<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="Reg_VPage.aspx.cs" Inherits="PowerWeb.Pages.Reg_VPage" %>
<%@ Register TagPrefix="pw" TagName="Reg_VModule" Src="~/Modules/Reg_VModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Reg_VModule runat="server" ID="mdlReg_VModule" />
</asp:Content>
