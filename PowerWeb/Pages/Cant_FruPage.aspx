<%@ Page Title="PowerWeb - Associazione unità fisse" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Cant_FruPage.aspx.cs" Inherits="PowerWeb.Pages.Cant_FruPage" %>

<%@ Register TagPrefix="pw" TagName="Cant_FruModule" Src="~/Modules/Cant_FruModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Cant_FruModule runat="server" ID="mdlCant_FruModule" />
</asp:Content>
