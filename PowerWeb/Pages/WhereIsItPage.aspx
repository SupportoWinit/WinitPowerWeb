<%@ Page Title="PowerWeb - Where is it" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="WhereIsItPage.aspx.cs" Inherits="PowerWeb.Pages.WhereIsItPage" %>

<%@ Register TagPrefix="pw" TagName="WhereIsItModule" Src="~/Modules/WhereIsItModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:WhereIsItModule runat="server" ID="mdlWhereIsIt" />
</asp:Content>
