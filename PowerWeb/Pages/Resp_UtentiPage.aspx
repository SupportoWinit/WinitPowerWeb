<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Resp_UtentiPage.aspx.cs" Inherits="PowerWeb.Pages.Resp_UtentiPage" %>
<%@ Register TagPrefix="pw" TagName="Resp_UtentiModule" Src="~/Modules/Resp_UtentiModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Resp_UtentiModule runat="server" ID="mdlResp_UtentiModule" />
</asp:Content>
