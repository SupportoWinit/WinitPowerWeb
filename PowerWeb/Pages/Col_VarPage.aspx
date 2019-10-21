<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Col_VarPage.aspx.cs" Inherits="PowerWeb.Pages.Col_VarPage" %>

<%@ Register TagPrefix="pw" TagName="Col_VarModule" Src="~/Modules/Col_VarModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Col_VarModule runat="server" ID="mdlCol_Var" />
</asp:Content>
