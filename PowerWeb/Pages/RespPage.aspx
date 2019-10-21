<%@ Page Title="PowerWeb - Responsabili" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="RespPage.aspx.cs" Inherits="PowerWeb.Pages.RespPage" %>

<%@ Register TagPrefix="pw" TagName="RespModule" Src="~/Modules/RespModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:RespModule runat="server" ID="mdlRespModule" />
</asp:Content>
